namespace quest.db;

public enum WorldStatus
{
    Initializing = 1,
    InProgress = 2,
    Finished = 3,
}

public enum ArtifactKind
{
    WorldHeader = 1,
    SettingLevels = 2,
    Population = 3,
    Backstory = 4,
    StoriesAndTasks = 5,
}

public enum ArtifactStage
{
    Initialization = 1,
    Enrichment = 2,
    Update = 3,
}

public enum ArtifactStatus
{
    Draft = 1,
    Approved = 2,
    Rejected = 3,
    Superseded = 4,
}
