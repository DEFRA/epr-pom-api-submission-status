namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionCreate;

using AutoMapper;
using Data.Entities.Submission;
using Data.Repositories.Commands.Interfaces;
using ErrorOr;
using MediatR;

public class SubmissionCreateCommandHandler :
    IRequestHandler<SubmissionCreateCommand, ErrorOr<SubmissionCreateResponse>>
{
    private readonly ICommandRepository<Submission> _commandRepository;
    private readonly IMapper _mapper;

    public SubmissionCreateCommandHandler(
        ICommandRepository<Submission> commandRepository, IMapper mapper)
    {
        _commandRepository = commandRepository;
        _mapper = mapper;
    }

    public async Task<ErrorOr<SubmissionCreateResponse>> Handle(
        SubmissionCreateCommand command, CancellationToken cancellationToken)
    {
        var submission = _mapper.Map<Submission>(command);

        await _commandRepository.AddAsync(submission);

        return await _commandRepository.SaveChangesAsync(cancellationToken)
            ? new SubmissionCreateResponse(submission.Id)
            : Error.Failure();
    }
}
