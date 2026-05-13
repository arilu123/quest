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
    World = 2,
    WorldPartL1 = 3,
    WorldPartL2 = 4,
    WorldPartL3 = 5,
    WorldPartL4 = 6,
    WorldPartL5 = 7,
    Population = 10,
    Backstory = 11,
    StoriesAndTasks = 12,
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
