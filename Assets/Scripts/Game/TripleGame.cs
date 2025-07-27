using System.Collections.Generic;
using UnityEngine;

public class TripleGame : AGame
{
    private InscriptionBlock inscriptionBlockYellow;

    private TargetBlock targetBlockYellow;

    public TripleGame(GameBoardGrid gameBoardGrid, int id, int stepsBest, int coinsBest, int stepsMinimum, int playedNumber, InscriptionBlock inscriptionBlockRed, InscriptionBlock inscriptionBlockBlue, InscriptionBlock inscriptionBlockYellow,
                        TargetBlock targetBlockRed, TargetBlock targeBlockBlue, TargetBlock targetBlockYellow, List<MobileBlock> mobileBlock, List<StaticBlock> staticBlocks, Stack<GameplayStep> allMoves)
                        : base(gameBoardGrid, id, stepsBest, coinsBest, stepsMinimum, playedNumber, inscriptionBlockRed, inscriptionBlockBlue, targetBlockRed, targeBlockBlue, mobileBlock, staticBlocks, allMoves)
    {
        this.inscriptionBlockYellow = inscriptionBlockYellow;
        this.targetBlockYellow = targetBlockYellow;
    }


    protected override void MoveUP()
    {
        bool isNewStepDone = false;

        List<ABlock> allMovableBlocks = new List<ABlock>();

        allMovableBlocks.Add(inscriptionBlockRed);
        allMovableBlocks.Add(inscriptionBlockBlue);
        allMovableBlocks.Add(inscriptionBlockYellow);

        if (mobileBlocks != null && mobileBlocks.Count > 0)
        {
            foreach (MobileBlock block in mobileBlocks)
            {
                allMovableBlocks.Add(block);
            }
        }

        for (int i = 0; i < allMovableBlocks.Count; i++)
        {
            foreach (ABlock block in allMovableBlocks)
            {
                if (!block.IsInTransit)
                {
                    if (block.CurrentTile.GetReferencePoint(ETileNeighborSide.Top) != null && block.CurrentTile.GetReferencePoint(ETileNeighborSide.Top).State == ETileState.Empty)
                    {
                        block.CurrentTile.GetReferencePoint(ETileNeighborSide.Top).SetFull();
                        block.CurrentTile.SetEmpty();
                        block.ChangePoint(block.CurrentTile.GetReferencePoint(ETileNeighborSide.Top));
                        block.TransitTransform();

                        if (!isNewStepDone)
                        {
                            isNewStepDone = true;
                        }
                    }
                }
            }
        }


        if (isNewStepDone)
        {
            isNewStepDone = false;

            stepsCounter++;

            if (backStepsCounter < backStepsMaximum)
            {
                backStepsCounter++;
            }

            SortedDictionary<int, Vector2> allBlocksPositions = new SortedDictionary<int, Vector2>();

            foreach (ABlock block in allMovableBlocks)
            {
                block.FinalTransform();

                allBlocksPositions.Add(block.Id, block.CurrentTile.AnchoredPosition);
            }

            GameplayStep nextStep = new GameplayStep(allMoves.Count, EDirection.Up, allBlocksPositions);

            allMoves.Push(nextStep);

            SoundManager.Instance.PlayStoneMove();
            ThrowFinalTransformEvent();
        }
    }

    protected override void MoveDOWN()
    {
        bool isNewStepDone = false;

        List<ABlock> allMovableBlocks = new List<ABlock>();

        allMovableBlocks.Add(inscriptionBlockRed);
        allMovableBlocks.Add(inscriptionBlockBlue);
        allMovableBlocks.Add(inscriptionBlockYellow);

        if (mobileBlocks != null && mobileBlocks.Count > 0)
        {
            foreach (MobileBlock block in mobileBlocks)
            {
                allMovableBlocks.Add(block);
            }
        }

        for (int i = 0; i < allMovableBlocks.Count; i++)
        {
            foreach (ABlock block in allMovableBlocks)
            {
                if (!block.IsInTransit)
                {
                    if (block.CurrentTile.GetReferencePoint(ETileNeighborSide.Bottom) != null && block.CurrentTile.GetReferencePoint(ETileNeighborSide.Bottom).State == ETileState.Empty)
                    {
                        block.CurrentTile.GetReferencePoint(ETileNeighborSide.Bottom).SetFull();
                        block.CurrentTile.SetEmpty();
                        block.ChangePoint(block.CurrentTile.GetReferencePoint(ETileNeighborSide.Bottom));
                        block.TransitTransform();

                        if (!isNewStepDone)
                        {
                            isNewStepDone = true;
                        }
                    }
                }
            }
        }


        if (isNewStepDone)
        {
            isNewStepDone = false;

            stepsCounter++;

            if (backStepsCounter < backStepsMaximum)
            {
                backStepsCounter++;
            }

            SortedDictionary<int, Vector2> allBlockPositions = new SortedDictionary<int, Vector2>();

            foreach (ABlock block in allMovableBlocks)
            {
                block.FinalTransform();

                allBlockPositions.Add(block.Id, block.CurrentTile.AnchoredPosition);
            }

            GameplayStep nextStep = new GameplayStep(allMoves.Count, EDirection.Down, allBlockPositions);

            allMoves.Push(nextStep);

            SoundManager.Instance.PlayStoneMove();
            ThrowFinalTransformEvent();
        }
    }

    protected override void MoveLEFT()
    {
        bool isNewStepDone = false;

        List<ABlock> allMovableBlocks = new List<ABlock>();

        allMovableBlocks.Add(inscriptionBlockRed);
        allMovableBlocks.Add(inscriptionBlockBlue);
        allMovableBlocks.Add(inscriptionBlockYellow);

        if (mobileBlocks != null && mobileBlocks.Count > 0)
        {
            foreach (MobileBlock block in mobileBlocks)
            {
                allMovableBlocks.Add(block);
            }
        }

        for (int i = 0; i < allMovableBlocks.Count; i++)
        {
            foreach (ABlock block in allMovableBlocks)
            {
                if (!block.IsInTransit)
                {
                    if (block.CurrentTile.GetReferencePoint(ETileNeighborSide.Left) != null && block.CurrentTile.GetReferencePoint(ETileNeighborSide.Left).State == ETileState.Empty)
                    {
                        block.CurrentTile.GetReferencePoint(ETileNeighborSide.Left).SetFull();
                        block.CurrentTile.SetEmpty();
                        block.ChangePoint(block.CurrentTile.GetReferencePoint(ETileNeighborSide.Left));
                        block.TransitTransform();

                        if (!isNewStepDone)
                        {
                            isNewStepDone = true;
                        }
                    }
                }
            }
        }


        if (isNewStepDone)
        {
            isNewStepDone = false;

            stepsCounter++;

            if (backStepsCounter < backStepsMaximum)
            {
                backStepsCounter++;
            }

            SortedDictionary<int, Vector2> allBlocksPositions = new SortedDictionary<int, Vector2>();

            foreach (ABlock block in allMovableBlocks)
            {
                block.FinalTransform();

                allBlocksPositions.Add(block.Id, block.CurrentTile.AnchoredPosition);
            }

            GameplayStep nextStep = new GameplayStep(allMoves.Count, EDirection.Left, allBlocksPositions);

            allMoves.Push(nextStep);

            SoundManager.Instance.PlayStoneMove();
            ThrowFinalTransformEvent();
        }
    }

    protected override void MoveRIGHT()
    {
        bool isNewStepDone = false;

        List<ABlock> allMovableBlocks = new List<ABlock>();

        allMovableBlocks.Add(inscriptionBlockRed);
        allMovableBlocks.Add(inscriptionBlockBlue);
        allMovableBlocks.Add(inscriptionBlockYellow);

        if (mobileBlocks != null && mobileBlocks.Count > 0)
        {
            foreach (MobileBlock block in mobileBlocks)
            {
                allMovableBlocks.Add(block);
            }
        }

        for (int i = 0; i < allMovableBlocks.Count; i++)
        {
            foreach (ABlock block in allMovableBlocks)
            {
                if (!block.IsInTransit)
                {
                    if (block.CurrentTile.GetReferencePoint(ETileNeighborSide.Right) != null && block.CurrentTile.GetReferencePoint(ETileNeighborSide.Right).State == ETileState.Empty)
                    {
                        block.CurrentTile.GetReferencePoint(ETileNeighborSide.Right).SetFull();
                        block.CurrentTile.SetEmpty();
                        block.ChangePoint(block.CurrentTile.GetReferencePoint(ETileNeighborSide.Right));
                        block.TransitTransform();

                        if (!isNewStepDone)
                        {
                            isNewStepDone = true;
                        }
                    }
                }
            }
        }


        if (isNewStepDone)
        {
            isNewStepDone = false;

            stepsCounter++;

            if (backStepsCounter < backStepsMaximum)
            {
                backStepsCounter++;
            }

            SortedDictionary<int, Vector2> allBlocksPositions = new SortedDictionary<int, Vector2>();

            foreach (ABlock block in allMovableBlocks)
            {
                block.FinalTransform();

                allBlocksPositions.Add(block.Id, block.CurrentTile.AnchoredPosition);
            }

            GameplayStep nextStep = new GameplayStep(allMoves.Count, EDirection.Right, allBlocksPositions);

            allMoves.Push(nextStep);

            SoundManager.Instance.PlayStoneMove();
            ThrowFinalTransformEvent();
        }
    }

    protected override void MoveBACK()
    {
        if (backStepsCounter == 0)
        {
            return;
        }

        backStepsCounter--;

        if (allMoves.Count > 1)
        {
            allMoves.Pop();

            GameplayStep prevStep = allMoves.Peek();

            stepsCounter--;

            List<ABlock> allMovableBlocks = new List<ABlock>();

            allMovableBlocks.Add(inscriptionBlockRed);
            allMovableBlocks.Add(inscriptionBlockBlue);
            allMovableBlocks.Add(inscriptionBlockYellow);

            if (mobileBlocks != null && mobileBlocks.Count > 0)
            {
                foreach (MobileBlock block in mobileBlocks)
                {
                    allMovableBlocks.Add(block);
                }
            }

            foreach (ABlock block in allMovableBlocks)
            {
                block.CurrentTile.SetEmpty();

                Vector2 blockPosition = prevStep.GetPositionById(block.Id);

                block.ChangePoint(gameBoardGrid[(int)blockPosition.x, (int)blockPosition.y]);
                block.CurrentTile.SetFull();
            }
        }

        SoundManager.Instance.PlayStoneMove();
        ThrowFinalTransformEvent();
    }

    protected override void StartStoneMatchEffects()
    {
        inscriptionBlockRed.StartStoneMatchEffects();
        inscriptionBlockBlue.StartStoneMatchEffects();
        inscriptionBlockYellow.StartStoneMatchEffects();
    }

    public override void PutBlockObjects()
    {
        List<ABlock> allBlocks = new List<ABlock>();

        allBlocks.Add(inscriptionBlockRed);
        allBlocks.Add(inscriptionBlockBlue);
        allBlocks.Add(inscriptionBlockYellow);

        allBlocks.Add(targetBlockRed);
        allBlocks.Add(targetBlockBlue);
        allBlocks.Add(targetBlockYellow);

        if (mobileBlocks != null && mobileBlocks.Count > 0)
        {
            foreach (MobileBlock block in mobileBlocks)
            {
                allBlocks.Add(block);
            }
        }

        if (staticBlocks != null && staticBlocks.Count > 0)
        {
            foreach (StaticBlock block in staticBlocks)
            {
                allBlocks.Add(block);
            }
        }

        foreach (ABlock block in allBlocks)
        {
            block.AnchoredPosition = block.CurrentTile.AnchoredPosition;
            block.Show();
        }
    }

    public override void RemoveBlockObjects()
    {
        List<ABlock> allBlocks = new List<ABlock>();

        allBlocks.Add(inscriptionBlockRed);
        allBlocks.Add(inscriptionBlockBlue);
        allBlocks.Add(inscriptionBlockYellow);

        allBlocks.Add(targetBlockRed);
        allBlocks.Add(targetBlockBlue);
        allBlocks.Add(targetBlockYellow);

        if (mobileBlocks != null && mobileBlocks.Count > 0)
        {
            foreach (MobileBlock block in mobileBlocks)
            {
                allBlocks.Add(block);
            }
        }

        if (staticBlocks != null && staticBlocks.Count > 0)
        {
            foreach (StaticBlock block in staticBlocks)
            {
                allBlocks.Add(block);
            }
        }

        foreach (ABlock block in allBlocks)
        {
            block.Hide();
        }
    }

    public override void MoveBlockObjects(float lerpAlpha, float minDistance)
    {
        List<ABlock> allBlocks = new List<ABlock>();

        allBlocks.Add(inscriptionBlockRed);
        allBlocks.Add(inscriptionBlockBlue);
        allBlocks.Add(inscriptionBlockYellow);

        if (mobileBlocks != null && mobileBlocks.Count > 0)
        {
            foreach (MobileBlock block in mobileBlocks)
            {
                allBlocks.Add(block);
            }
        }

        foreach (ABlock block in allBlocks)
        {
            if (Vector3.Distance(block.AnchoredPosition, block.CurrentTile.AnchoredPosition) >= minDistance)
            {
                block.AnchoredPosition = Vector3.Lerp(block.AnchoredPosition, block.CurrentTile.AnchoredPosition, lerpAlpha);
            }
            else
            {
                block.AnchoredPosition = block.CurrentTile.AnchoredPosition;
            }
        }


        foreach (ABlock block in allBlocks)
        {
            if (block.AnchoredPosition != block.CurrentTile.AnchoredPosition)
            {
                return;
            }
        }

        ThrowTransitOverEvent();

        if (inscriptionBlockRed.CurrentTile == targetBlockRed.CurrentTile && inscriptionBlockBlue.CurrentTile == targetBlockBlue.CurrentTile && inscriptionBlockYellow.CurrentTile == targetBlockYellow.CurrentTile)
        {
            SoundManager.Instance.PlayStoneStop();
            StartStoneMatchEffects();
            ThrowStonesMatchEvent();
            return;
        }

        if (stepsCounter > GameStartData.MaximumStepsCount)
        {
            ThrowErrorEvent(EErrorType.StepsCount);
        }
    }

    public override Board GetBoardState()
    {
        int coloredPawnsCount = 3;
        bool hasMobileBlocks = mobileBlocks != null;
        bool hasStaticBlocks = staticBlocks != null;
        int mobileBlocksCount = hasMobileBlocks ? mobileBlocks.Count : 0;
        int staticBlocksCount = hasStaticBlocks ? staticBlocks.Count : 0;

        Piece[] targets = new Piece[coloredPawnsCount];
        targets[0] = new Piece(targetBlockRed.CurrentTile.Position, EPawnColor.Red);
        targets[1] = new Piece(targetBlockBlue.CurrentTile.Position, EPawnColor.Blue);
        targets[2] = new Piece(targetBlockYellow.CurrentTile.Position, EPawnColor.Green);

        Pawn[] pawns = new Pawn[coloredPawnsCount + mobileBlocks.Count + staticBlocks.Count];
        pawns[0] = new Pawn(inscriptionBlockRed.CurrentTile.Position, EPawnColor.Red);
        pawns[1] = new Pawn(inscriptionBlockBlue.CurrentTile.Position, EPawnColor.Blue);
        pawns[2] = new Pawn(inscriptionBlockYellow.CurrentTile.Position, EPawnColor.Green);

        if (hasMobileBlocks)
        {
            for (int i = coloredPawnsCount; i < mobileBlocksCount + coloredPawnsCount; i++)
            {
                pawns[i] = new Pawn(mobileBlocks[i].CurrentTile.Position, EPawnColor.Grey);
            }
        }

        Block[] blocks = new Block[staticBlocksCount];

        if (hasStaticBlocks)
        {
            for (int i = 0; i < staticBlocksCount; i++)
            {
                blocks[i] = new Block(staticBlocks[i].CurrentTile.Position);
            }
        }

        return new Board(gameBoardGrid.Width, gameBoardGrid.Height, pawns, blocks, targets);
    }
}
