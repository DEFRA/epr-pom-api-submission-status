namespace EPR.SubmissionMicroservice.API.MockServices;

using System.Diagnostics.CodeAnalysis;
using Common.Functions.CancellationTokens.Interfaces;

[ExcludeFromCodeCoverage]
public class MockCancellationTokenAccessor : ICancellationTokenAccessor
{
    public CancellationToken CancellationToken { get; set; }
}