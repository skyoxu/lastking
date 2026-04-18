using System.Collections.Generic;
using Game.Core.Repositories;

namespace Game.Core.Services;

public sealed class AchievementDefinitionLoader
{
    private readonly AchievementConfigRepository repository;

    public AchievementDefinitionLoader()
        : this(new AchievementConfigRepository())
    {
    }

    public AchievementDefinitionLoader(AchievementConfigRepository repository)
    {
        this.repository = repository;
    }

    public IReadOnlyList<AchievementDefinitionRecord> LoadAtStartup(string configPath)
    {
        return repository.LoadDefinitions(configPath);
    }
}
