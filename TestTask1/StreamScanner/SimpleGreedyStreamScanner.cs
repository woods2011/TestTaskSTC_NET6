﻿using System.Buffers;
using System.Text.RegularExpressions;
using TestTask1.Helpers;

namespace TestTask1.StreamScanner;

public class SimpleGreedyStreamScanner : StreamScannerBase
{
    /// <inheritdoc/>
    public SimpleGreedyStreamScanner(Action<RangeMatch>? matchNotifier = null) : base(matchNotifier) { }


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
        var savedRangeParts = new SimpleRangePartsAggregator(readBufferSize, scanParams);

        var isFindingStart = true;
        var prevReminderLen = 0;

        minReadSize = Math.Clamp(minReadSize, 1, readBufferSize);
        int bytesRead;

        while ((bytesRead = await stream.ReadAtLeastAsync(
                   buffer: savedRangeParts.CurrentReadBuffer.AsMemory(prevReminderLen, readBufferSize),
                   minimumBytes: minReadSize,
                   throwOnEndOfStream: false,
                   token)) > 0)
        {
            Memory<byte> newDataPartWithPrevReminder =
                savedRangeParts.CurrentReadBuffer.AsMemory(0, prevReminderLen + bytesRead);

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

                if (RangeIsNotValidLength(scanParams.StartLen + savedRangeParts.TotalLen + dataPart.Length))
                {
                    var remainLength = Math.Min(scanParams.Max, dataPart.Length);
                    FindingStartInRemainData(dataPart.Slice(dataPart.Length - remainLength));
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
                int remainLength = Math.Min(scanParams.Max, indexOfEnd);
                FindingStartInRemainData(dataPart.Slice(indexOfEnd - remainLength));
                return;
            }

            savedRangeParts.ConstructRangeAndProcess(
                scanParams.StartBytes, dataPart[..(indexOfEnd + scanParams.EndLen)],
                ProcessRange);

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


        void SetNewReminder(ReadOnlyMemory<byte> data, int maxReminderLength)
        {
            prevReminderLen = Math.Min(maxReminderLength, data.Length);
            int indexToSlice = data.Length - prevReminderLen;
            data[indexToSlice..].CopyTo(savedRangeParts.CurrentReadBuffer);
        }

        void SetStartReminder(ReadOnlyMemory<byte> data) => SetNewReminder(data, scanParams.StartLen - 1);


        void SetNewReminderAndSavePart(ReadOnlyMemory<byte> data, int maxReminderLength)
        {
            prevReminderLen = Math.Min(maxReminderLength, data.Length);
            int indexOfReminder = data.Length - prevReminderLen;

            savedRangeParts.AddNewPart(data[..indexOfReminder]);
            data[indexOfReminder..].CopyTo(savedRangeParts.CurrentReadBuffer);
        }

        void SetEndReminderAndSavePart(ReadOnlyMemory<byte> data) =>
            SetNewReminderAndSavePart(data, scanParams.EndLen - 1);


        void ProcessRange(ReadOnlyMemory<byte> rangeData)
        {
            if (RangeDoNotIncludesContains(rangeData)) return;

            // Тут тоже можно использовать массив из пула, но тогда нельзя использовать ValueMatch, т.к. будем ссылаться
            // на память которая будет перезаписана, следовательно если совпадений в диапазоне мало, а сам диапазон
            // большой лучше использовать массив из пула и обычный Match, иначе ValueMatch
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


    private class SimpleRangePartsAggregator
    {
        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        private readonly List<byte[]> _rentedArrays = new();
        private readonly int _rangePartReadBufferSize;

        private readonly List<ReadOnlyMemory<byte>> _rangeParts = new();

        public SimpleRangePartsAggregator(int baseBufferSize, StreamScanParams scanParams)
        {
            _rangePartReadBufferSize = baseBufferSize + Math.Max(scanParams.StartLen, scanParams.EndLen);
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

// reader.AdvanceTo(dataSequence.End);