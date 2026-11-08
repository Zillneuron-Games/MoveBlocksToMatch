using System;

public interface ILevelSelector
{
    bool Unlocked(int level);
    void Select(int level);
}
