using System.Buffers;
using System.Text.RegularExpressions;
using TestTask1.Helpers;
using TestTask1.StreamScanner;

namespace TestTask1.Benchmarks.ObsoleteStreamScannerImplementations;

public class OldDefaultStreamScanner : StreamScannerBase
{
    /// <inheritdoc/>
    public OldDefaultStreamScanner(Action<RangeMatch>? matchNotifier = null) : base(matchNotifier) { }

    
    /// <inheritdoc/>
    public override async Task ScanStreamAsync(
        Stream stream,
        StreamScanParams scanParams,
        int readBufferSize = 4096,
        CancellationToken token = default) => await
        ScanStreamAsync(
            stream,
            scanParams,
            readBufferSize,
            minReadSize: readBufferSize,
            token);


    public async Task ScanStreamAsync(
        Stream stream,
        StreamScanParams scanParams,
        int readBufferSize = 4096,
        int minReadSize = 4096,
        CancellationToken token = default)
    {
        var savedRangeParts = new RangePartsAggregator(readBufferSize, scanParams);

        var (isFindingStart, isInRewindMode) = (true, false);
        var prevReminderLen = 0;

        minReadSize = Math.Clamp(minReadSize, 1, readBufferSize);

        while (true)
        {
            if (!isInRewindMode)
            {
                byte[] currentReadBuffer = savedRangeParts.CurrentReadBuffer;

                int bytesRead = await stream.ReadAtLeastAsync(
                    currentReadBuffer.AsMemory(prevReminderLen, readBufferSize), minReadSize, false, token);

                if (bytesRead == 0) break;

                Memory<byte> newDataPartWithPrevReminder = currentReadBuffer.AsMemory(0, prevReminderLen + bytesRead);

                if (isFindingStart)
                    FindStartInNewData(newDataPartWithPrevReminder);
                else
                    FindEndInNewData(newDataPartWithPrevReminder);
            }
            else
            {
                ReadOnlyMemory<byte> rangeWithRemovedStart = savedRangeParts.ConstructRangeWithoutStart();
                FindStartInAccumulatedData(rangeWithRemovedStart);

                isInRewindMode = false;
            }
        }

        return;

        // -------------------------------------------------------------------------------------------------------------
        void FindStartInNewData(ReadOnlyMemory<byte> dataPart)
        {
            int indexOfStart = dataPart.Span.IndexOf(scanParams.StartBytes, scanParams.IgnoreCase);
            if (indexOfStart is -1)
            {
                SetStartReminder(dataPart);
                return;
            }

            (isFindingStart, prevReminderLen) = (false, 0);
            FindEndInNewData(dataPart[(indexOfStart + scanParams.StartLen)..]);
        }

        void FindEndInNewData(ReadOnlyMemory<byte> dataPart)
        {
            if (dataPart.Length == 0) return;

            int indexOfEnd = dataPart.Span.IndexOf(scanParams.EndBytes, scanParams.IgnoreCase);
            if (indexOfEnd is -1) // Конец диапазона не найден
            {
                if (RangeIncludesForbiddenSymbol(dataPart, out int indexOfSymbol1))
                {
                    FindingStartInRemainData(dataPart[(indexOfSymbol1 + 1)..]);
                    return;
                }

                if (RangeIsNotValidLength(scanParams.StartLen + savedRangeParts.TotalLen + dataPart.Length))
                {
                    savedRangeParts.AddNewPart(dataPart);
                    savedRangeParts.TrimToFitMaxLength(scanParams.Max);
                    (isFindingStart, isInRewindMode) = (true, true);
                }
                else SetEndReminderAndSavePart(dataPart);

                return;
            }

            // Конец диапазона найден
            if (RangeIncludesForbiddenSymbol(dataPart[..indexOfEnd], out int indexOfSymbol2))
            {
                FindingStartInRemainData(dataPart[(indexOfSymbol2 + 1)..]);
                return;
            }

            if (RangeIsNotValidLength(scanParams.StartLen + savedRangeParts.TotalLen + indexOfEnd + scanParams.EndLen))
            {
                savedRangeParts.AddLastPart(dataPart, indexOfEnd);
                savedRangeParts.TrimToFitMaxLength(scanParams.Max); // max - ?
                (isFindingStart, isInRewindMode) = (true, true);    // мб сразу вызвать метод поиска
                return;
            }

            ReadOnlyMemory<byte> constructedRange = savedRangeParts
                .ConstructRange(scanParams.StartBytes, dataPart[..(indexOfEnd + scanParams.EndLen)]);
            ProcessRange(constructedRange);

            FindingStartInRemainData(dataPart[(indexOfEnd + scanParams.EndLen)..]);
            return;

            // ---------------------------------------------------------------------------------------------------------
            void FindingStartInRemainData(ReadOnlyMemory<byte> remainData)
            {
                savedRangeParts.DropAccumulatedParts();
                (isFindingStart, prevReminderLen) = (true, 0);
                FindStartInNewData(remainData);
            }
        }


        void FindStartInAccumulatedData(ReadOnlyMemory<byte> data)
        {
            int indexOfStart = data.Span.IndexOf(scanParams.StartBytes, scanParams.IgnoreCase);
            if (indexOfStart is -1)
            {
                SetStartReminderAndDropAllParts(data);
                return;
            }

            (isFindingStart, prevReminderLen) = (false, 0);
            savedRangeParts.Slice(indexOfStart + scanParams.StartLen);
            FindEndInAccumulatedData(data[(indexOfStart + scanParams.StartLen)..]);
        }

        void FindEndInAccumulatedData(ReadOnlyMemory<byte> data)
        {
            if (data.Length == 0) return;

            if (savedRangeParts.SavedEndPos >= 0)
            {
                int savedEndPos = savedRangeParts.SavedEndPos.Value;

                ReadOnlyMemory<byte> constructedRange1 =
                    MemoryHelpers.JoinMemory(scanParams.StartBytes, data[..(savedEndPos + scanParams.EndLen)]);
                ProcessRange(constructedRange1);

                savedRangeParts.Slice(savedEndPos + scanParams.EndLen);
                FindingStartInRemainAccumulatedData(data[(savedEndPos + scanParams.EndLen)..]);
                return;
            }

            int indexOfEnd = data.Span.IndexOf(scanParams.EndBytes, scanParams.IgnoreCase);
            if (indexOfEnd is -1) // Конец диапазона не найден
            {
                // Если не нашли конец и диапазон стал не валидным, срезаем и начанием искать начало
                if (RangeIncludesForbiddenSymbol(data, out int indexOfSymbol1))
                {
                    savedRangeParts.Slice(indexOfSymbol1 + 1);
                    FindingStartInRemainAccumulatedData(data[(indexOfSymbol1 + 1)..]);
                    return;
                }

                if (RangeIsNotValidLength(scanParams.StartLen + data.Length))
                {
                    savedRangeParts.TrimToFitMaxLength(scanParams.Max);
                    int sliceStartIndex = Math.Max(0, data.Length - scanParams.Max);
                    FindingStartInRemainAccumulatedData(data[sliceStartIndex..]);
                    return;
                }

                // Если не нашли конец, но диапазон все еще валидный, устанавливаем остаток и выходим из перемотки
                SetEndReminderAndTrimLastPartEnding(data);
                return;
            }

            // Конец диапазона найден
            if (RangeIncludesForbiddenSymbol(data[..indexOfEnd], out int indexOfSymbol2))
            {
                savedRangeParts.Slice(indexOfSymbol2 + 1);
                FindingStartInRemainAccumulatedData(data[(indexOfSymbol2 + 1)..]);
                return;
            }

            if (RangeIsNotValidLength(scanParams.StartLen + indexOfEnd + scanParams.EndLen))
            {
                savedRangeParts.SavedEndPos = indexOfEnd;
                savedRangeParts.TrimToFitMaxLength(scanParams.Max);
                int sliceStartIndex = Math.Max(0, indexOfEnd + scanParams.EndLen - scanParams.Max);
                FindingStartInRemainAccumulatedData(data[sliceStartIndex..]);
                return;
            }

            ReadOnlyMemory<byte> constructedRange2 =
                MemoryHelpers.JoinMemory(scanParams.StartBytes, data[..(indexOfEnd + scanParams.EndLen)]);
            ProcessRange(constructedRange2);

            savedRangeParts.Slice(indexOfEnd + scanParams.EndLen);
            FindingStartInRemainAccumulatedData(data[(indexOfEnd + scanParams.EndLen)..]);
            return;

            // ---------------------------------------------------------------------------------------------------------
            void FindingStartInRemainAccumulatedData(ReadOnlyMemory<byte> remainData)
            {
                (isFindingStart, prevReminderLen) = (true, 0);
                FindStartInAccumulatedData(remainData);
            }
        }


        void SetNewReminder(ReadOnlyMemory<byte> data, int maxReminderLength)
        {
            prevReminderLen = Math.Min(maxReminderLength, data.Length);
            int indexToSlice = data.Length - prevReminderLen;
            data[indexToSlice..].CopyTo(savedRangeParts.CurrentReadBuffer);
        }

        void SetStartReminder(ReadOnlyMemory<byte> data) => SetNewReminder(data, scanParams.StartLen - 1);

        void SetStartReminderAndDropAllParts(ReadOnlyMemory<byte> data)
        {
            SetStartReminder(data);
            savedRangeParts.DropAccumulatedParts();
        }


        void SetNewReminderAndSavePart(ReadOnlyMemory<byte> data, int maxReminderLength)
        {
            prevReminderLen = Math.Min(maxReminderLength, data.Length);
            int indexOfReminder = data.Length - prevReminderLen;

            savedRangeParts.AddNewPart(data[..indexOfReminder]);
            data[indexOfReminder..].CopyTo(savedRangeParts.CurrentReadBuffer);
        }

        void SetEndReminderAndSavePart(ReadOnlyMemory<byte> data) =>
            SetNewReminderAndSavePart(data, scanParams.EndLen - 1);


        void SetNewReminderEndTrimEndAccumulatedRange(ReadOnlyMemory<byte> data, int maxReminderLength)
        {
            prevReminderLen = Math.Min(maxReminderLength, data.Length);
            int indexToSlice = data.Length - prevReminderLen;

            savedRangeParts.TrimLastPartEnding(prevReminderLen);
            data[indexToSlice..].CopyTo(savedRangeParts.CurrentReadBuffer);
        }

        void SetEndReminderAndTrimLastPartEnding(ReadOnlyMemory<byte> data) =>
            SetNewReminderEndTrimEndAccumulatedRange(data, scanParams.EndLen - 1);


        void ProcessRange(ReadOnlyMemory<byte> rangeData)
        {
            if (RangeDoNotIncludesContains(rangeData)) return;

            char[] range = AsciiHelpers.ConvertToChars(rangeData.Span);

            var matches = new List<ScanMatch>();
            foreach (ValueMatch match in scanParams.Regex.EnumerateMatches(range))
                matches.Add(new ScanMatch(range.AsMemory(match.Index, match.Length), match.Index));

            MatchNotifier?.Invoke(new RangeMatch(matches, range.Length));
        }

        bool RangeIsNotValidLength(int rangeLength) =>
            rangeLength > scanParams.Max;

        bool RangeIncludesForbiddenSymbol(ReadOnlyMemory<byte> rangeData, out int index)
        {
            index = rangeData.Span.IndexOfAny(Nul, (byte)'\t');
            if (index > -1)
            {
                
            }
            return index is not -1;
        }

        bool RangeDoNotIncludesContains(ReadOnlyMemory<byte> rangeData) =>
            rangeData.Span.IndexOf(scanParams.ContainsBytes, scanParams.IgnoreCase) is -1;
    }


    /// <summary>
    /// Накапливает уже прочитанные части в ходе поиска конца диапазона,
    /// <para>Используется для возможности повторного поиска нового старта диапазона в уже прочитанных данных,
    /// если стало понятно что длина текущего диапазона превышает допустимую длину.</para>
    /// </summary>
    private class RangePartsAggregator
    {
        private readonly ArrayPool<byte> _arrayPool;
        private readonly List<byte[]> _rentedArrays = new();
        private readonly int _rangePartReadBufferSize;

        private readonly List<ReadOnlyMemory<byte>> _rangeParts = new();

        private readonly int _endLen;

        public RangePartsAggregator(int baseBufferSize, StreamScanParams scanParams)
        {
            _endLen = scanParams.EndLen;

            int maxRangeLenForBuckets = Math.Min(scanParams.Max, 8 * 1024 * 1024);
            _rangePartReadBufferSize = baseBufferSize + Math.Max(scanParams.StartLen, scanParams.EndLen);
            _arrayPool =
                ArrayPool<byte>.Create(_rangePartReadBufferSize, maxRangeLenForBuckets / _rangePartReadBufferSize + 2);

            CurrentReadBuffer = _arrayPool.Rent(_rangePartReadBufferSize);
        }

        
        public byte[] CurrentReadBuffer { get; private set; }
        
        public int TotalLen => _rangeParts.Sum(part => part.Length);

        public int? SavedEndPos { get; set; }

        
        public void AddNewPart(ReadOnlyMemory<byte> dataPart)
        {
            // Сохраняем текущий буффер в список арендованных массивов
            _rentedArrays.Add(CurrentReadBuffer);

            // Сохраняем часть диапазона без остатка прошлой итерации 
            _rangeParts.Add(dataPart);

            // Создаем(арендуем) новый буфер для чтения
            CurrentReadBuffer = _arrayPool.Rent(_rangePartReadBufferSize);
        }

        public void AddLastPart(ReadOnlyMemory<byte> dataPart, int endPos)
        {
            SavedEndPos = TotalLen + endPos;
            AddNewPart(dataPart);
        }

        public void DropAccumulatedParts()
        {
            _rentedArrays.ForEach(rentedArray => _arrayPool.Return(rentedArray));
            _rentedArrays.Clear();
            _rangeParts.Clear();
            SavedEndPos = null;
        }

        
        public void Slice(int start)
        {
            int indexOfFirstValidData = start;
            if (indexOfFirstValidData <= 0) return;

            SavedEndPos -= indexOfFirstValidData;

            int countToRemove = 0;
            int curLengthSum = 0;

            for (int i = 0; i < _rangeParts.Count; i++)
            {
                ReadOnlyMemory<byte> curPart = _rangeParts[i];
                curLengthSum += curPart.Length;

                if (indexOfFirstValidData <= curLengthSum)
                {
                    _rangeParts[i] = curPart.Slice(curPart.Length - (curLengthSum - indexOfFirstValidData));
                    countToRemove = i;
                    if (_rangeParts[i].Length is 0) countToRemove++;
                    break;
                }
            }

            _rangeParts.RemoveRange(0, countToRemove);

            List<byte[]> arraysToReturn = _rentedArrays.Take(countToRemove).ToList();
            arraysToReturn.ForEach(rentedArray => _arrayPool.Return(rentedArray));
            _rentedArrays.RemoveRange(0, countToRemove);
            
            // if (SavedEndPos < 0) SavedEndPos = null;
        }

        public void TrimLastPartEnding(int countToTrim)
        {
            ReadOnlyMemory<byte> lastPart = _rangeParts[^1];
            countToTrim = Math.Min(lastPart.Length, countToTrim);
            _rangeParts[^1] = lastPart.Slice(0, lastPart.Length - countToTrim);

            if (_rangeParts[^1].Length is not 0) return;

            _rangeParts.RemoveAt(_rangeParts.Count - 1);
            _arrayPool.Return(_rentedArrays[^1]);
            _rentedArrays.RemoveAt(_rentedArrays.Count - 1);
        }

        public void TrimToFitMaxLength(int maxLength)
        {
            if (SavedEndPos < 0) SavedEndPos = null;
            int indexOfFirstValidData = (SavedEndPos + _endLen ?? TotalLen) - maxLength;
            Slice(indexOfFirstValidData);
        }

        
        public Memory<byte> ConstructRange(ReadOnlyMemory<byte> firstPart, ReadOnlyMemory<byte> lastPart) =>
            MemoryHelpers.JoinMemory(firstPart, _rangeParts, lastPart);

        public Memory<byte> ConstructRangeWithoutStart() =>
            MemoryHelpers.JoinMemory(_rangeParts);
    }

    private const byte Nul = 0;
    private const byte Lf = 10;
    private const byte Cr = 13;
}

// reader.AdvanceTo(dataSequence.End);