using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zillneuron.UILayout;

public class GameplayManager : MonoBehaviour, ITileObjectProvider
{
    #region Events

    public event Action<int, int> onStartGame;
    public event Action<int> onStepsUpdate;
    public event Action<EDirection> onShowHint;
    public event Action onShowSettings;
    public event Action<int, int> onShowMenu;
    public event Action<EErrorType> onShowError;
    public event Action<int, int, int, bool> onWinGame;
    public event Action onEndGame;
    public event Action onQuitGame;

    #endregion Events

    #region Variables

    private float gameEndPauseTime;
    private float blocksMoveSpeed;
    private float blocksMinDistance;
    private EDirection lastHint;
    private EErrorType lastError;

    #region Inspector

    public GameObject inscriptionRed;
    public GameObject inscriptionBlue;
    public GameObject inscriptionYellow;
    public GameObject inscriptionGreen;

    public GameObject targetRed;
    public GameObject targetBlue;
    public GameObject targetYellow;
    public GameObject targetGreen;

    public GameObject[] mobileBlocks;
    public GameObject[] staticBlocks;

    public GameObjectRow[] gridBlocks;

    #endregion Inspector

    private EGameplayState gameplayState;
    private GameStartData gameStartData;
    private GameBoardGrid gameBoardGrid;

    private AGame currentGame;

    #endregion Variables

    private void Awake()
    {
        gameplayState = EGameplayState.Start;
    }

    private void Start()
    {
        ViewContext.Instance.Construct();
    }

    private void Update()
    {
        switch (gameplayState)
        {
            case EGameplayState.Gameplay: UpdateGame(); break;
            case EGameplayState.Transit: UpdateTransit(); break;
            case EGameplayState.Pause: UpdatePause(); break;
        }
    }

    public void StartGame()
    {
        StartCoroutine(LoadScene());
    }

    private IEnumerator LoadScene()
    {
        gameStartData = GameStartData.CreateInstance();

        blocksMoveSpeed = 16.0f;
        blocksMinDistance = 0.05f;

        SoundManager.Instance.Switch(gameStartData.SoundState);
        InputManager.Instance.eventInput += HandleInputEvent;

        gameBoardGrid = new GameBoardGrid(7, 5, this);

        CreateGame(gameStartData.NextToLoadGame);

        yield return new WaitForEndOfFrame();

        InputManager.Instance.Enable();
    }

    private void ChangeGameplayState(EGameplayState gameplayState)
    {
        this.gameplayState = gameplayState;

        switch (gameplayState)
        {
            case EGameplayState.Gameplay:; break;
            case EGameplayState.Pause:;break;
            case EGameplayState.End: ; break;
            case EGameplayState.Error: UIManager.Instance.ChangeGameplayState(this.gameplayState, InputManager.Instance.Enable); break;
            case EGameplayState.Win: UIManager.Instance.Disable(); UIManager.Instance.ChangeGameplayState(this.gameplayState, null); break;
            case EGameplayState.Transit: UIManager.Instance.Disable(); UIManager.Instance.ChangeGameplayState(this.gameplayState, null); break;
        }
    }

    private void CreateGame(int gameId)
    {
        if (currentGame != null)
        {
            DestroyGame();
            gameBoardGrid.Clear();
        }

        HideAllElements();

        GameData gameData = gameStartData.GetGameData(gameId);

        GameDataDynamic gameDataDynamic = gameStartData.GetGameDataDynamic(gameId);

        if (gameData.IsDoubleGame)
        {
            int indexCounter = 1;

            SortedDictionary<int, Vector2Int> allBlocksPositions = new SortedDictionary<int, Vector2Int>();

            InscriptionBlock inscriptionBlockRed = new InscriptionBlock(indexCounter, inscriptionRed, gameBoardGrid[(int)gameData.Red.x, (int)gameData.Red.y]);
            allBlocksPositions.Add(indexCounter, gameData.Red);
            indexCounter++;

            InscriptionBlock inscriptionBlockBlue = new InscriptionBlock(indexCounter, inscriptionBlue, gameBoardGrid[(int)gameData.Blue.x, (int)gameData.Blue.y]);
            allBlocksPositions.Add(indexCounter, gameData.Blue);
            indexCounter++;

            TargetBlock targetBlockRed = new TargetBlock(indexCounter, targetRed, gameBoardGrid[(int)gameData.RedTarget.x, (int)gameData.RedTarget.y]);
            indexCounter++;

            TargetBlock targetBlockBlue = new TargetBlock(indexCounter, targetBlue, gameBoardGrid[(int)gameData.BlueTarget.x, (int)gameData.BlueTarget.y]);
            indexCounter++;

            List<MobileBlock> mobileBlocks = null;

            if (gameData.Mobiles != null && gameData.Mobiles.Count > 0)
            {
                mobileBlocks = new List<MobileBlock>();

                int mobileBlocksIndexCounter = 0;

                foreach (Vector2Int pos in gameData.Mobiles)
                {
                    MobileBlock tempBlock = new MobileBlock(indexCounter, this.mobileBlocks[mobileBlocksIndexCounter], gameBoardGrid[(int)pos.x, (int)pos.y]);
                    mobileBlocks.Add(tempBlock);
                    allBlocksPositions.Add(indexCounter, pos);
                    indexCounter++;
                    mobileBlocksIndexCounter++;
                }
            }

            List<StaticBlock> staticBlocks = null;

            if (gameData.Statics != null && gameData.Statics.Count > 0)
            {
                staticBlocks = new List<StaticBlock>();

                int staticBlocksIndexCounter = 0;

                foreach (Vector2 pos in gameData.Statics)
                {
                    StaticBlock tempBlock = new StaticBlock(indexCounter, this.staticBlocks[staticBlocksIndexCounter], gameBoardGrid[(int)pos.x, (int)pos.y]);
                    staticBlocks.Add(tempBlock);
                    indexCounter++;
                    staticBlocksIndexCounter++;
                }
            }

            Stack<GameplayStep> allStepsContainer = new Stack<GameplayStep>();
            allStepsContainer.Push(new GameplayStep(0, EDirection.None, allBlocksPositions));

            currentGame = new DoubleGame(gameBoardGrid, gameData.Id, gameDataDynamic.BestSteps, gameDataDynamic.BestCoins, gameData.MinimumStepsCount, gameDataDynamic.GameCount, inscriptionBlockRed, inscriptionBlockBlue, targetBlockRed, targetBlockBlue, mobileBlocks, staticBlocks, allStepsContainer);
        }
        else
        {
            int indexCounter = 1;

            GameObject inscriptionBlockThird = null;
            GameObject targetThird = null;
            ChooseRandomBlock(gameData.Id, out inscriptionBlockThird, out targetThird);

            SortedDictionary<int, Vector2Int> allBlocksPositions = new SortedDictionary<int, Vector2Int>();

            InscriptionBlock inscriptionBlockRed = new InscriptionBlock(indexCounter, inscriptionRed, gameBoardGrid[(int)gameData.Red.x, (int)gameData.Red.y]);
            allBlocksPositions.Add(indexCounter, gameData.Red);
            indexCounter++;

            InscriptionBlock inscriptionBlockBlue = new InscriptionBlock(indexCounter, inscriptionBlue, gameBoardGrid[(int)gameData.Blue.x, (int)gameData.Blue.y]);
            allBlocksPositions.Add(indexCounter, gameData.Blue);
            indexCounter++;

            InscriptionBlock inscriptionBlockGreen = new InscriptionBlock(indexCounter, inscriptionBlockThird, gameBoardGrid[(int)gameData.Green.x, (int)gameData.Green.y]);
            allBlocksPositions.Add(indexCounter, gameData.Green);
            indexCounter++;

            TargetBlock targetBlockRed = new TargetBlock(indexCounter, targetRed, gameBoardGrid[(int)gameData.RedTarget.x, (int)gameData.RedTarget.y]);
            indexCounter++;

            TargetBlock targetBlockBlue = new TargetBlock(indexCounter, targetBlue, gameBoardGrid[(int)gameData.BlueTarget.x, (int)gameData.BlueTarget.y]);
            indexCounter++;

            TargetBlock targetBlockGreen = new TargetBlock(indexCounter, targetThird, gameBoardGrid[(int)gameData.GreenTarget.x, (int)gameData.GreenTarget.y]);
            indexCounter++;

            List<MobileBlock> mobileBlocks = null;

            if (gameData.Mobiles != null && gameData.Mobiles.Count > 0)
            {
                mobileBlocks = new List<MobileBlock>();

                int mobileBlocksIndexCounter = 0;

                foreach (Vector2Int pos in gameData.Mobiles)
                {
                    MobileBlock tempBlock = new MobileBlock(indexCounter, this.mobileBlocks[mobileBlocksIndexCounter], gameBoardGrid[(int)pos.x, (int)pos.y]);
                    mobileBlocks.Add(tempBlock);
                    allBlocksPositions.Add(indexCounter, pos);
                    indexCounter++;
                    mobileBlocksIndexCounter++;
                }
            }

            List<StaticBlock> staticBlocks = null;

            if (gameData.Statics != null && gameData.Statics.Count > 0)
            {
                staticBlocks = new List<StaticBlock>();

                int staticBlocksIndexCounter = 0;

                foreach (Vector2 pos in gameData.Statics)
                {
                    StaticBlock tempBlock = new StaticBlock(indexCounter, this.staticBlocks[staticBlocksIndexCounter], gameBoardGrid[(int)pos.x, (int)pos.y]);
                    staticBlocks.Add(tempBlock);
                    indexCounter++;
                    staticBlocksIndexCounter++;
                }
            }

            Stack<GameplayStep> allStepsContainer = new Stack<GameplayStep>();
            allStepsContainer.Push(new GameplayStep(0, EDirection.None, allBlocksPositions));

            currentGame = new TripleGame(gameBoardGrid, gameData.Id, gameDataDynamic.BestSteps, gameDataDynamic.BestCoins, gameData.MinimumStepsCount, gameDataDynamic.GameCount, inscriptionBlockRed, inscriptionBlockBlue, inscriptionBlockGreen, targetBlockRed, targetBlockBlue, targetBlockGreen, mobileBlocks, staticBlocks, allStepsContainer);
        }

        currentGame.PutBlockObjects();

        currentGame.eventFinalTransform += HandleFinalTransform;
        currentGame.eventBlocksMatch += HandleBlocksMatch;
        currentGame.eventTransitOver += HandleTransitOver;
        currentGame.eventError += HandleError;

        UIManager.Instance.CreateGame(currentGame.MinimumStepsCount, currentGame.BestStepsCount, currentGame.BestCoinsCount);

        OnStartGame();
        ChangeGameplayState(EGameplayState.Gameplay);
    }

    private void DestroyGame()
    {
        currentGame.RemoveBlockObjects();

        currentGame.eventFinalTransform -= HandleFinalTransform;
        currentGame.eventBlocksMatch -= HandleBlocksMatch;
        currentGame.eventTransitOver -= HandleTransitOver;
        currentGame.eventError -= HandleError;

        currentGame = null;
    }

    private void EndGame()
    {
        OnEndGame();
        ChangeGameplayState(EGameplayState.End);
    }

    private void ChooseRandomBlock(int gameId, out GameObject currentBlock, out GameObject currentTarget)
    {
        if (gameId % 2 == 0)
        {
            currentBlock = inscriptionGreen;
            currentTarget = targetGreen;
        }
        else
        {
            currentBlock = inscriptionYellow;
            currentTarget = targetYellow;
        }
    }

    #region Update methods

    private void UpdateGame()
    {

    }

    private void UpdateTransit()
    {
        currentGame.MoveBlockObjects(Time.deltaTime * blocksMoveSpeed, blocksMinDistance);
    }

    private void UpdatePause()
    {

    }

    #endregion Update methods

    #region Handlers

    private void HandleInputEvent(object sender, InputEventArgs args)
    {
        if (gameplayState == EGameplayState.Transit)
        {
            return;
        }

        switch (args.InputEvent)
        {
            case EInputEvent.Up:
                if (gameplayState == EGameplayState.Gameplay)
                {
                    currentGame.MoveBlocks(EDirection.Up);
                }
                break;

            case EInputEvent.Down:
                if (gameplayState == EGameplayState.Gameplay)
                {
                    currentGame.MoveBlocks(EDirection.Down);
                }
                break;

            case EInputEvent.Left:
                if (gameplayState == EGameplayState.Gameplay)
                {
                    currentGame.MoveBlocks(EDirection.Left);
                }
                break;

            case EInputEvent.Right:
                if (gameplayState == EGameplayState.Gameplay)
                {
                    currentGame.MoveBlocks(EDirection.Right);
                }
                break;

            case EInputEvent.Back:
                if (gameplayState == EGameplayState.Gameplay)
                {
                    currentGame.MoveBlocks(EDirection.None);
                }
                break;

            case EInputEvent.Escape:
                switch (gameplayState)
                {
                    case EGameplayState.Gameplay: currentGame.MoveBlocks(EDirection.None); break;
                    case EGameplayState.Pause: OnStartGame(); ChangeGameplayState(EGameplayState.Gameplay); break;
                    case EGameplayState.End: HandleMenuButtonClick(); break;
                }
                break;

            case EInputEvent.Hint:
                HandleHintButtonClick();
                break;

            case EInputEvent.Menu:
                HandleMenuButtonClick();
                break;

            case EInputEvent.Next:
                HandleNextButtonClick();
                break;

            case EInputEvent.Settings:
                HandleSettingsButtonClick();
                break;

            case EInputEvent.Play:
                HandlePlayButtonClick();
                break;

            case EInputEvent.Reload:
                HandleReloadButtonClick();
                break;
            case EInputEvent.Quit:
                HandleQuitClick();
                break;

        }
    }

    private void HandleFinalTransform(object sender, EventArgs args)
    {
        ChangeGameplayState(EGameplayState.Transit);
    }

    private void HandleTransitOver(object sender, EventArgs args)
    {
        OnStepsUpdate();
        ChangeGameplayState(EGameplayState.Gameplay);
    }

    private void HandleBlocksMatch(object sender, EventArgs args)
    {
        OnWinGame();

        ChangeGameplayState(EGameplayState.Win);

        gameStartData.UpdateDynamicData(currentGame.DynamicData);

        UIManager.Instance.UpdateGameInfo(currentGame.StepsCount, currentGame.BestStepsCount, currentGame.CoinsCount, currentGame.BestCoinsCount);

        Invoke("EndGame", gameEndPauseTime);
        //Invoke("OnShowMenu", gameEndPauseTime);
    }

    private void HandleError(object sender, GameErrorEventArgs args)
    {
        lastError = args.ErrorType;

        OnShowError();
        ChangeGameplayState(EGameplayState.Error);
    }

    private void HandleHintButtonClick()
    {
        lastHint = GetHint();

        OnShowHint();
    }

    private void HandleMenuButtonClick()
    {
        OnShowMenu();
        ChangeGameplayState(EGameplayState.Pause);
    }

    private void HandleNextButtonClick()
    {
        gameStartData.NextToLoadGame = gameStartData.NextToLoadGame + 1;
        CreateGame(gameStartData.NextToLoadGame);
    }

    private void HandleSettingsButtonClick()
    {
        OnShowSettings();
        ChangeGameplayState(EGameplayState.Pause);
    }

    private void HandlePlayButtonClick()
    {
        OnStartGame();
        ChangeGameplayState(EGameplayState.Gameplay);
    }

    private void HandleReloadButtonClick()
    {
        CreateGame(gameStartData.NextToLoadGame);
    }

    private void HandleQuitClick()
    {
        OnQuitGame();
    }

    #endregion

    #region Evennts

    private void OnStartGame()
    {
        Action<int, int> temp = onStartGame;

        if (temp != null)
        {
            temp(currentGame.Id, currentGame.StepsCount);
        }
    }

    private void OnStepsUpdate()
    {
        Action<int> temp = onStepsUpdate;

        if (temp != null)
        {
            temp(currentGame.StepsCount);
        }
    }

    private void OnShowHint()
    {
        Action<EDirection> temp = onShowHint;

        if (temp != null)
        {
            temp(lastHint);
        }
    }

    private void OnShowMenu()
    {
        Action<int, int> temp = onShowMenu;

        if (temp != null)
        {
            temp(currentGame.MinimumStepsCount, currentGame.BestStepsCount);
        }
    }

    private void OnShowSettings()
    {
        Action temp = onShowSettings;

        if (temp != null)
        {
            temp();
        }
    }

    private void OnShowError()
    {
        Action<EErrorType> temp = onShowError;

        if (temp != null)
        {
            temp(lastError);
        }
        
    }
    
    private void OnWinGame()
    {
        Action<int, int, int, bool> temp = onWinGame;

        if (temp != null)
        {
            temp(currentGame.StepsCount, currentGame.BestStepsCount, currentGame.MinimumStepsCount, gameStartData.NextToLoadGame <= GameStartData.GamesCount);
        }
    }

    private void OnEndGame()
    {
        Action temp = onEndGame;

        if (temp != null)
        {
            temp();
        }
    }

    private void OnQuitGame()
    {
        Action temp = onQuitGame;

        if (temp != null)
        {
            temp();
        }        
    }

    #endregion Events

    public GameObject GetTileObject(int x, int y)
    {
        return gridBlocks[y].elements[x];
    }

    private void HideAllElements()
    {
        inscriptionRed.SetActive(false);
        inscriptionBlue.SetActive(false);
        inscriptionYellow.SetActive(false);
        inscriptionGreen.SetActive(false);

        targetRed.SetActive(false);
        targetBlue.SetActive(false);
        targetYellow.SetActive(false);
        targetGreen.SetActive(false);

        for (int i = 0; i < mobileBlocks.Length; i++)
        {
            mobileBlocks[i].SetActive(false);
        }

        for (int i = 0; i < staticBlocks.Length; i++)
        {
            staticBlocks[i].SetActive(false);
        }
    }

    public EDirection GetHint()
    {
        if (currentGame != null)
        {
            Board board = currentGame.GetBoardState();
            Hint hintManager = new Hint(board);
            Vector2Int direction = hintManager.GetAStarHint();

            if (direction == Direction2D.Up)
            {
                return EDirection.Up;
            }
            else if (direction == Direction2D.Down)
            {
                return EDirection.Down;
            }
            else if (direction == Direction2D.Left)
            {
                return EDirection.Left;
            }
            else if (direction == Direction2D.Right)
            {
                return EDirection.Right;
            }
        }

        return EDirection.None;
    }
}
