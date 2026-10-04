namespace SpyBrowser.Cursory.Internal;

/// <summary>Ranks nearby recordings and draws one using Cursory's directness preference.</summary>
internal static class TrajectorySelection
{
    private const int NearestCount = 5;
    private const int PerturbedQueryCount = 20;
    private const double EfficiencyFloor = 2.220446049250313e-16;

    internal static (Recording Record, int Index) Select(
        Recording[] dataset, TrajectoryPoint start, TrajectoryPoint end,
        double distance, double directness, RandomSampler random, Action<string, object?>? trace = null)
    {
        var candidateIndices = new List<int>();
        AddNearest(dataset, start, end, NearestCount, candidateIndices);

        double perturbation = distance * 0.1;
        for (int query = 0; query < PerturbedQueryCount; query++)
        {
            var perturbedEnd = new TrajectoryPoint(
                end.X + random.Uniform(-perturbation, perturbation),
                end.Y + random.Uniform(-perturbation, perturbation));
            AddNearest(dataset, start, perturbedEnd, NearestCount, candidateIndices);
        }

        double[] efficiencies = dataset.Select(Efficiency).Order().ToArray();
        double preferredRank = 0.2 + 0.75 * directness;
        var weights = new double[candidateIndices.Count];
        for (int i = 0; i < weights.Length; i++)
            weights[i] = CandidateWeight(dataset[candidateIndices[i]], efficiencies, preferredRank);

        int selectedCandidate = random.Choice(weights);
        int selectedIndex = candidateIndices[selectedCandidate];
        double totalWeight = weights.Sum();
        var normalizedWeights = weights.Select(weight => weight / totalWeight).ToArray();
        trace?.Invoke("selection", new
        {
            candidateRecordingIndices = candidateIndices,
            weights = normalizedWeights,
            selectedCandidate,
            selectedRecordingIndex = selectedIndex
        });
        return (dataset[selectedIndex], selectedIndex);
    }

    private static void AddNearest(
        Recording[] dataset, TrajectoryPoint start, TrajectoryPoint target, int count, List<int> candidates)
    {
        double dx = target.X - start.X;
        double dy = target.Y - start.Y;
        double targetLength = NumericCompat.Hypot(dx, dy);
        var scores = new (double Score, int Index)[dataset.Length];

        for (int i = 0; i < dataset.Length; i++)
        {
            Recording recording = dataset[i];
            double recordingDx = recording.Points[^1].X - recording.Points[0].X;
            double recordingDy = recording.Points[^1].Y - recording.Points[0].Y;
            double recordingLength = NumericCompat.Hypot(recordingDx, recordingDy);
            double score = targetLength == 0
                ? recording.Length
                : 0.8 * (1 - (recordingLength == 0 ? 0 : (recordingDx * dx + recordingDy * dy) / (recordingLength * targetLength)))
                    + 0.2 * Math.Abs(recordingLength - targetLength) / Math.Max(targetLength, 1);
            scores[i] = (score, i);
        }

        Array.Sort(scores, static (left, right) =>
        {
            int scoreOrder = left.Score.CompareTo(right.Score);
            return scoreOrder != 0 ? scoreOrder : left.Index.CompareTo(right.Index);
        });
        for (int i = 0; i < count; i++) candidates.Add(scores[i].Index);
    }

    private static double CandidateWeight(Recording recording, double[] sortedEfficiencies, double preferredRank)
    {
        double efficiency = Efficiency(recording);
        int rank = Array.BinarySearch(sortedEfficiencies, efficiency);
        if (rank < 0) rank = ~rank;
        else while (rank < sortedEfficiencies.Length && sortedEfficiencies[rank] <= efficiency) rank++;

        double normalizedRank = rank / (double)sortedEfficiencies.Length;
        double offset = (normalizedRank - preferredRank) / 0.2;
        double centeredRank = 2 * normalizedRank - 1;
        return Math.Exp(-0.5 * offset * offset) + 0.05 * (1 + 0.1 * Math.Pow(Math.Abs(centeredRank), 3));
    }

    private static double Efficiency(Recording recording)
    {
        double dx = recording.Points[^1].X - recording.Points[0].X;
        double dy = recording.Points[^1].Y - recording.Points[0].Y;
        return NumericCompat.Hypot(dx, dy) / Math.Max(recording.Length, EfficiencyFloor);
    }
}
