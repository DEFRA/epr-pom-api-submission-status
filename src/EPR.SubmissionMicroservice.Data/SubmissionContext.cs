using System.Diagnostics.CodeAnalysis;
using EPR.Common.Functions.AccessControl.Interfaces;
using EPR.Common.Functions.Database.Context;
using EPR.Common.Functions.Database.Decorators.Interfaces;
using EPR.Common.Functions.Database.Entities;
using EPR.Common.Functions.Database.Entities.Interfaces;
using EPR.Common.Functions.Services.Interfaces;
using EPR.SubmissionMicroservice.Data.Converters;
using EPR.SubmissionMicroservice.Data.Entities;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;
using EPR.SubmissionMicroservice.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Data;

[ExcludeFromCodeCoverage]
public class SubmissionContext : EprCommonContext
{
    private readonly IRequestTimeService _requestTimeService;

    public SubmissionContext(
        DbContextOptions contextOptions,
        IUserContextProvider userContextProvider,
        IRequestTimeService requestTimeService,
        IEnumerable<IEntityDecorator> entityDecorators)
        : base(contextOptions, userContextProvider, requestTimeService, entityDecorators)
    {
        _requestTimeService = requestTimeService;
    }

    public DbSet<Submission> Submissions { get; set; }

    public DbSet<AbstractSubmissionEvent> Events { get; set; }

    public DbSet<AbstractValidationError> ValidationEventErrors { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries<EntityWithId>())
        {
            if (entry is { State: EntityState.Added, Entity: ICreated created })
            {
                created.Created = _requestTimeService.UtcRequest;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureApplicationKeys(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Submission>()
            .ToContainer("Submissions")
            .HasPartitionKey(x => x.Id)
            .HasNoDiscriminator();

        modelBuilder.Entity<AbstractValidationEvent>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<AntivirusCheckEvent>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<RegulatorPoMDecisionEvent>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<RegulatorRegistrationDecisionEvent>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<AntivirusResultEvent>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<CheckSplitterValidationEvent>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<ProducerValidationEvent>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<SubmittedEvent>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<RegistrationValidationEvent>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<BrandValidationEvent>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<PartnerValidationEvent>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<CheckSplitterValidationError>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<CheckSplitterValidationWarning>()
            .HasPartitionKey(x => x.ValidationEventId);

        modelBuilder.Entity<ProducerValidationError>()
            .HasPartitionKey(x => x.Id);

        modelBuilder.Entity<ProducerValidationWarning>()
            .HasPartitionKey(x => x.ValidationEventId);

        modelBuilder.Entity<RegistrationValidationError>()
            .HasPartitionKey(x => x.Id);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Submission>(x =>
        {
            x.Property(e => e.Id).ToJsonProperty("SubmissionId");
            x.Property(e => e.SubmissionType).HasConversion<string>();
            x.Property(e => e.DataSourceType).HasConversion<string>();
        });

        modelBuilder.Entity<AbstractSubmissionEvent>(x =>
        {
            x.ToContainer("SubmissionEvents");
            x.HasPartitionKey(submissionEvent => submissionEvent.Id);
            x.HasDiscriminator(submissionEvent => submissionEvent.Type)
                .HasValue<CheckSplitterValidationEvent>(EventType.CheckSplitter)
                .HasValue<ProducerValidationEvent>(EventType.ProducerValidation)
                .HasValue<RegistrationValidationEvent>(EventType.Registration)
                .HasValue<BrandValidationEvent>(EventType.BrandValidation)
                .HasValue<PartnerValidationEvent>(EventType.PartnerValidation)
                .HasValue<AntivirusCheckEvent>(EventType.AntivirusCheck)
                .HasValue<AntivirusResultEvent>(EventType.AntivirusResult)
                .HasValue<RegulatorPoMDecisionEvent>(EventType.RegulatorPoMDecision)
                .HasValue<RegulatorRegistrationDecisionEvent>(EventType.RegulatorRegistrationDecision)
                .HasValue<SubmittedEvent>(EventType.Submitted);
            x.Property(e => e.Id).ToJsonProperty("SubmissionEventId");
            x.Property(e => e.Type).HasConversion<string>();
        });

        modelBuilder.Entity<RegulatorPoMDecisionEvent>(x =>
        {
            x.Property(e => e.Decision).HasConversion<string>();
        });

        modelBuilder.Entity<RegulatorRegistrationDecisionEvent>(x =>
        {
            x.Property(e => e.Decision).HasConversion<string>();
        });

        modelBuilder.Entity<AbstractValidationError>(x =>
        {
            x.ToContainer("ProducerValidationErrors");
            x.HasPartitionKey(submissionEventError => submissionEventError.Id);
            x.HasDiscriminator(submissionEventError => submissionEventError.ValidationErrorType)
                .HasValue<CheckSplitterValidationError>(ValidationType.CheckSplitter)
                .HasValue<ProducerValidationError>(ValidationType.ProducerValidation)
                .HasValue<RegistrationValidationError>(ValidationType.Registration);
            x.Property(e => e.Id).ToJsonProperty("ProducerValidationErrorId");
            x.Property(e => e.ValidationErrorType).HasConversion<string>();
        });

        modelBuilder.Entity<AbstractValidationWarning>(x =>
        {
            x.ToContainer("ProducerValidationWarnings");
            x.HasPartitionKey(warning => warning.ValidationEventId);
            x.HasDiscriminator(warning => warning.ValidationWarningType)
                .HasValue<ProducerValidationWarning>(ValidationType.ProducerValidation)
                .HasValue<CheckSplitterValidationWarning>(ValidationType.CheckSplitter);
            x.Property(e => e.Id).ToJsonProperty("ProducerValidationWarningId");
            x.Property(e => e.ValidationWarningType).HasConversion<string>();
        });

        modelBuilder.Entity<AntivirusCheckEvent>()
            .Property(e => e.FileType).HasConversion<string>();

        modelBuilder.Entity<AntivirusResultEvent>()
            .Property(e => e.AntivirusScanResult).HasConversion<string>();

        modelBuilder.Entity<Migration>(x =>
        {
            x.HasNoKey();
            x.ToTable("__EFMigrationsHistory", t => t.ExcludeFromMigrations());
        });

        modelBuilder.ApplyUtcDateTimeConverter();

        ConfigureApplicationKeys(modelBuilder);
    }
}