namespace Game.Core.Services;

public enum CloudConflictChoice
{
    None = 0,
    Local = 1,
    Cloud = 2,
}

public sealed record CloudSaveSnapshot(string Revision, string Payload);

public sealed record CloudConflictDecision(
    bool RequiresUserDecision,
    bool AppliedLocalForThisOperation,
    bool AppliedCloudForThisOperation,
    bool CloudOverwriteScheduled);

public sealed class CloudSaveConflictResolver
{
    public CloudConflictDecision Resolve(
        CloudSaveSnapshot localSnapshot,
        CloudSaveSnapshot cloudSnapshot,
        CloudConflictChoice choice)
    {
        var hasConflict = localSnapshot.Revision != cloudSnapshot.Revision;
        if (!hasConflict)
        {
            return new CloudConflictDecision(
                RequiresUserDecision: false,
                AppliedLocalForThisOperation: true,
                AppliedCloudForThisOperation: false,
                CloudOverwriteScheduled: false);
        }

        if (choice == CloudConflictChoice.Local)
        {
            return new CloudConflictDecision(
                RequiresUserDecision: false,
                AppliedLocalForThisOperation: true,
                AppliedCloudForThisOperation: false,
                CloudOverwriteScheduled: false);
        }

        if (choice == CloudConflictChoice.Cloud)
        {
            return new CloudConflictDecision(
                RequiresUserDecision: false,
                AppliedLocalForThisOperation: false,
                AppliedCloudForThisOperation: true,
                CloudOverwriteScheduled: false);
        }

        return new CloudConflictDecision(
            RequiresUserDecision: true,
            AppliedLocalForThisOperation: false,
            AppliedCloudForThisOperation: false,
            CloudOverwriteScheduled: false);
    }
}
