namespace EPR.SubmissionMicroservice.API.UnitTests;

using System.Collections;
using ErrorOr;

public class ErrorsTestData : IEnumerable<Error>
{
    public IEnumerator<Error> GetEnumerator()
    {
        yield return Error.Conflict();
        yield return Error.Validation();
        yield return Error.NotFound();
        yield return Error.Failure();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}