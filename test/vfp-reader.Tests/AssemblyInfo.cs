using Xunit;

// The suite shares the internal ambient Storage.Current seam (D44): a FakeStorage scope
// installed by one test would otherwise intercept another test's path-based VfpTable.Open
// while xUnit runs the two classes in parallel. The suite is small and fast, so run the
// tests serially and keep the ambient seam deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
