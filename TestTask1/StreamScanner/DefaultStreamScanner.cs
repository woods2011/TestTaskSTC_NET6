using System.Buffers;
using System.Text.RegularExpressions;
using TestTask1.Helpers;

namespace TestTask1.StreamScanner;

public class DefaultStreamScanner : StreamScannerBase
{
    /// <inheritdoc/>
    public DefaultStreamScanner(Action<RangeMatch>? matchNotifier = null) : base(matchNotifier) { }


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

        bool isFindingStart = true;
        var prevReminderLen = 0;

        minReadSize = Math.Clamp(minReadSize, 1, readBufferSize);

        while (true)
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

                savedRangeParts.AddNewPart(dataPart);

                if (RangeIsNotValidLength(scanParams.StartLen + savedRangeParts.TotalLen))
                {
                    savedRangeParts.TrimToFitMaxLength(scanParams.Max - 1); // ?: (- 1)
                    if (!FindStartInAccumulatedData(out int remainDataCount))
                    {
                        savedRangeParts.DropAccumulatedParts();
                        return;
                    }

                    remainDataCount = Math.Min(remainDataCount, dataPart.Length);
                    dataPart = dataPart.Slice(dataPart.Length - remainDataCount, remainDataCount);
                }

                SetEndReminderAndTrimLastPart(dataPart);
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
                savedRangeParts.AddNewPart(dataPart[..indexOfEnd]);
                savedRangeParts.TrimToFitMaxLength(scanParams.Max - scanParams.EndLen); // ?: (- scanParams.EndLen)
                if (!FindStartInAccumulatedData(out _))
                {
                    FindingStartInReallocatedRemainData(dataPart[indexOfEnd..]);
                    return;
                }

                savedRangeParts.ConstructRangeAndProcess(
                    scanParams.StartBytes, scanParams.EndBytes, ProcessRange);
            }
            else
            {
                savedRangeParts.ConstructRangeAndProcess(
                    scanParams.StartBytes, dataPart[..(indexOfEnd + scanParams.EndLen)], ProcessRange);
            }

            prevReminderLen = 0;
            FindingStartInReallocatedRemainData(dataPart[(indexOfEnd + scanParams.EndLen)..]);
            return;

            // ---------------------------------------------------------------------------------------------------------
            void FindingStartInRemainData(ReadOnlyMemory<byte> remainData)
            {
                savedRangeParts.DropAccumulatedParts();

                (isFindingStart, prevReminderLen) = (true, 0);
                FindStartInNewData(remainData);
            }

            void FindingStartInReallocatedRemainData(ReadOnlyMemory<byte> remainData)
            {
                remainData = remainData.CopyToAndFitDest(
                    savedRangeParts.CurrentReadBuffer.AsMemory(prevReminderLen, remainData.Length));
                savedRangeParts.DropAccumulatedParts();

                isFindingStart = true;
                FindStartInNewData(savedRangeParts.CurrentReadBuffer.AsMemory(0, prevReminderLen + remainData.Length));
            }
        }

        bool FindStartInAccumulatedData(out int remainDataCount)
        {
            remainDataCount = 0;
            isFindingStart = true;

            var rangeAndMemoryOwner = savedRangeParts.ConstructRangeWithoutStart();
            ReadOnlyMemory<byte> data = rangeAndMemoryOwner.Range;
            using IMemoryOwner<byte> memoryOwner = rangeAndMemoryOwner.MemoryOwner;

            int indexOfStart = data.Span.IndexOf(scanParams.StartBytes, scanParams.IgnoreCase);
            if (indexOfStart is -1)
            {
                SetStartReminder(data);
                return false;
            }

            (isFindingStart, prevReminderLen) = (false, 0);
            int indexToSlice = indexOfStart + scanParams.StartLen;
            remainDataCount = data.Length - indexToSlice;
            savedRangeParts.Slice(indexToSlice);
            return true;
        }


        void SetNewReminder(ReadOnlyMemory<byte> data, int maxReminderLength)
        {
            prevReminderLen = Math.Min(maxReminderLength, data.Length);
            int indexOfReminder = data.Length - prevReminderLen;
            data[indexOfReminder..].CopyTo(savedRangeParts.CurrentReadBuffer);
        }

        void SetStartReminder(ReadOnlyMemory<byte> data) =>
            SetNewReminder(data, scanParams.StartLen - 1);


        void SetNewReminderAndTrimLastPart(ReadOnlyMemory<byte> data, int maxReminderLength)
        {
            SetNewReminder(data, maxReminderLength);
            savedRangeParts.TrimLastPartEnding(prevReminderLen);
        }

        void SetEndReminderAndTrimLastPart(ReadOnlyMemory<byte> data) =>
            SetNewReminderAndTrimLastPart(data, scanParams.EndLen - 1);


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
            // Вообще говоря лучше вынести запретные символы в ScanParams
            // index = rangeData.Span.IndexOfAny(Nul, Cr, Lf);
            index = rangeData.Span.IndexOfAny(Nul, Tab);
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
        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        private readonly List<byte[]> _rentedArrays = new();
        private readonly int _rangePartReadBufferSize;

        private readonly List<ReadOnlyMemory<byte>> _rangeParts = new();

        public RangePartsAggregator(int baseBufferSize, StreamScanParams scanParams)
        {
            _rangePartReadBufferSize = baseBufferSize + (scanParams.StartLen - 1) + (scanParams.EndLen - 1);
            CurrentReadBuffer = _arrayPool.Rent(_rangePartReadBufferSize);
        }

        public byte[] CurrentReadBuffer { get; private set; }

        public int TotalLen => _rangeParts.Sum(part => part.Length);


        public void AddNewPart(ReadOnlyMemory<byte> dataPart)
        {
            // Сохраняем текущий буффер в список арендованных массивов
            _rentedArrays.Add(CurrentReadBuffer);

            // Сохраняем часть диапазона без остатка прошлой итерации 
            _rangeParts.Add(dataPart);

            // Создаем(арендуем) новый буфер для чтения
            CurrentReadBuffer = _arrayPool.Rent(_rangePartReadBufferSize);
        }

        public void DropAccumulatedParts()
        {
            _rentedArrays.ForEach(rentedArray => _arrayPool.Return(rentedArray));
            _rentedArrays.Clear();
            _rangeParts.Clear();
        }


        public void Slice(int start)
        {
            if (start <= 0) return;

            int countToRemove = 0;
            int curLengthSum = 0;

            for (int i = 0; i < _rangeParts.Count; i++)
            {
                ReadOnlyMemory<byte> curPart = _rangeParts[i];
                curLengthSum += curPart.Length;

                if (start <= curLengthSum)
                {
                    _rangeParts[i] = curPart.Slice(curPart.Length - (curLengthSum - start));
                    countToRemove = i;
                    if (_rangeParts[i].Length is 0) countToRemove++;
                    break;
                }
            }

            _rangeParts.RemoveRange(0, countToRemove);

            List<byte[]> arraysToReturn = _rentedArrays.Take(countToRemove).ToList();
            arraysToReturn.ForEach(rentedArray => _arrayPool.Return(rentedArray));
            _rentedArrays.RemoveRange(0, countToRemove);
        }

        public void TrimLastPartEnding(int countToTrim)
        {
            if (countToTrim is 0) return;

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
            int indexOfFirstValidData = TotalLen - maxLength;
            Slice(indexOfFirstValidData);
        }


        public (Memory<byte> Range, IMemoryOwner<byte> MemoryOwner) ConstructRangeWithoutStart()
        {
            IMemoryOwner<byte> constructRangeBuffer = MemoryPool<byte>.Shared.Rent(TotalLen);
            return (MemoryHelpers.JoinMemory(_rangeParts, constructRangeBuffer.Memory), constructRangeBuffer);
        }


        public void ConstructRangeAndProcess(
            ReadOnlyMemory<byte> firstPart,
            ReadOnlyMemory<byte> lastPart,
            Action<ReadOnlyMemory<byte>> rangeProcessor)
        {
            byte[] constructRangeBuffer = ArrayPool<byte>.Shared.Rent(firstPart.Length + TotalLen + lastPart.Length);

            ReadOnlyMemory<byte> range =
                MemoryHelpers.JoinMemory(firstPart, _rangeParts, lastPart, constructRangeBuffer);
            rangeProcessor(range);

            ArrayPool<byte>.Shared.Return(constructRangeBuffer);
        }
    }

    private const byte Nul = 0;
    private const byte Lf = 10;
    private const byte Cr = 13;
    private const byte Tab = (byte) '\t';
}