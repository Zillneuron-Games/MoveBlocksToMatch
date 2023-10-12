using System;
using System.Collections.Generic;

public abstract class AGame
{
    #region Consts

    protected const int MaxStepsCount = GlobalParameters.MaximumStepsCount;

    #endregion Consts

    #region Events

    public event EventHandler<EventArgs> eventStonesMatch;
    public event EventHandler<EventArgs> eventFinalTransform;
    public event EventHandler<EventArgs> eventTransitOver;
    public event EventHandler<GameErrorEventArgs> eventError;

    #endregion Events

    #region Variables

    protected int id;
    protected int puzzleId;
    protected int stepsCounter;
    protected int coinsCounter;

    protected int stepsMinimum;
    protected int playedNumber;

    protected int backStepsCounter;
    protected int backStepsMaximum;

    protected InscriptionBlock inscriptionBlockRed;
    protected InscriptionBlock inscriptionBlockBlue;

    protected TargetBlock targetBlockRed;
    protected TargetBlock targetBlockBlue;

    protected List<MobileBlock> mobileBlocks;
    protected List<StaticBlock> staticBlocks;

    protected Stack<GameplayStep> allMoves;

    protected GameBoardGrid gameBoardGrid;
    protected GameDataDynamic gameDataDynamic;

    #endregion Variables

    #region Properties

    public int Id
    {
        get { return id; }
    }

    public int PuzzleId
    {
        get { return puzzleId; }
    }

    public int PlayedCount
    {
        get { return playedNumber; }
    }

    public int StepsCount
    {
        get { return stepsCounter; }
    }

    public int CoinsCount
    {
        get { return coinsCounter; }
    }

    public int BestStepsCount
    {
        get { return gameDataDynamic.BestSteps; }
    }

    public int BestCoinsCount
    {
        get { return gameDataDynamic.BestCoins; }
    }

    public int MinimumStepsCount
    {
        get { return stepsMinimum; }
    }

    public int BackStepsCount
    {
        get { return backStepsCounter; }
    }

    public GameDataDynamic DynamicData
    {
        get
        {
            return gameDataDynamic;
        }
    }

    #endregion Properties

    #region Constructors

    public AGame(GameBoardGrid gameBoardGrid, int id, int puzzleId, int stepsBest, int coinsBest, int stepsMinimum, int playedNumber, InscriptionBlock inscriptionBlockRed, InscriptionBlock inscriptionBlockBlue,
                        TargetBlock targetBlockRed, TargetBlock targetBlockBlue, List<MobileBlock> mobileBlocks, List<StaticBlock> staticBlocks, Stack<GameplayStep> allMoves)
    {
        this.gameBoardGrid = gameBoardGrid;
        this.id = id;
        this.puzzleId = puzzleId;
        this.stepsCounter = 0;
        this.coinsCounter = 0;

        this.stepsMinimum = stepsMinimum;
        this.playedNumber = playedNumber;

        this.backStepsCounter = 0;
        this.backStepsMaximum = MaxStepsCount;

        this.inscriptionBlockRed = inscriptionBlockRed;
        this.inscriptionBlockBlue = inscriptionBlockBlue;

        this.targetBlockRed = targetBlockRed;
        this.targetBlockBlue = targetBlockBlue;

        this.mobileBlocks = mobileBlocks;
        this.staticBlocks = staticBlocks;

        this.gameDataDynamic = new GameDataDynamic(id, stepsBest, coinsBest, playedNumber + 1);

        this.allMoves = allMoves;
    }

    #endregion Constructors

    #region Methods

    public void MoveStones(EDirection direction)
    {
        switch (direction)
        {
            case EDirection.Up: MoveUP(); break;
            case EDirection.Down: MoveDOWN(); break;
            case EDirection.Left: MoveLEFT(); break;
            case EDirection.Right: MoveRIGHT(); break;
            case EDirection.None: MoveBACK(); break;
        }
    }

    protected void ThrowFinalTransformEvent()
    {
        EventHandler<EventArgs> temp_final_transform_event = eventFinalTransform;

        if (temp_final_transform_event != null)
            temp_final_transform_event(this, new EventArgs());

    }


    protected void ThrowStonesMatchEvent()
    {
        gameDataDynamic.UpdateBestStep(stepsCounter);
        CalculateCoins();
        gameDataDynamic.UpdateBestCoins(coinsCounter);

        EventHandler<EventArgs> temp_stone_match_event = eventStonesMatch;

        if (temp_stone_match_event != null)
            temp_stone_match_event(this, new EventArgs());

    }

    protected void ThrowTransitOverEvent()
    {
        EventHandler<EventArgs> temp_transform_over_event = eventTransitOver;

        if (temp_transform_over_event != null)
            temp_transform_over_event(this, new EventArgs());

    }

    protected void ThrowErrorEvent(EErrorType _error_key)
    {
        EventHandler<GameErrorEventArgs> temp_error_event = eventError;

        if (temp_error_event != null)
            temp_error_event(this, new GameErrorEventArgs(_error_key));

    }

    protected void CalculateCoins()
    {
        float level_number = id;
        float game_minimal_steps = stepsMinimum;
        float player_steps = StepsCount;

        if (stepsMinimum > StepsCount)
        {
            //BugReport.Instance.MinimumSteps(id, stepsMinimum, StepsCount);

            game_minimal_steps = StepsCount;
            player_steps = stepsMinimum;
        }

        float main_factor_float = level_number + 3 * game_minimal_steps - player_steps;

        coinsCounter = main_factor_float > (level_number / 2.0f) ? (int)main_factor_float : (int)(level_number / 2.0f);
    }

    protected abstract void MoveUP();

    protected abstract void MoveDOWN();

    protected abstract void MoveLEFT();

    protected abstract void MoveRIGHT();

    protected abstract void MoveBACK();

    public abstract void PutStoneObjects();

    public abstract void RemoveStoneObjects();

    public abstract void MoveStoneObjects(float _lerp_alpha, float _min_distance);

    protected abstract void StartStoneMatchEffects();

    #endregion Methods

}