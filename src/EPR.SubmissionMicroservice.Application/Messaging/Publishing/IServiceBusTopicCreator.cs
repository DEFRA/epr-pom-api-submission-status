namespace EPR.SubmissionMicroservice.Application.Messaging.Publishing;

public interface IServiceBusTopicCreator
{
    Task ConfigureTopics();
}