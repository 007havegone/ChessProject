using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using UnityEngine;
/// <summary>
/// 悔棋类
/// </summary>
public class ChessReseting
{
    private GameManager gameManager;
    // 计数器，用来计数当前一共走了几步棋
    public int resetCount=0;
    //悔棋数组，用来存放所有已经走过的步数，用来悔棋
    public ChessStep[] chessSteps;
    public ChessReseting()
    {
        gameManager = GameManager.Instance;
    }
    /// <summary>
    /// 棋子位置索引
    /// </summary>
    public struct ChessIndex
    {
        public int x;
        public int y;
    }
    /// <summary>
    /// 记录每一步悔棋的具体结构体
    /// </summary>
    public struct ChessStep
    {
        public ChessIndex from;//起始位置索引信息
        public ChessIndex to;//目标位置索引信息
        public GameObject gridOne;//起始位置所在格子
        public GameObject gridTwo;//目标位置所在格子
        public GameObject chessOne;//起始位置棋子对象
        public GameObject chessTwo;//目标位置游戏对象
        public int chessOneID;//起始位置棋子ID
        public int chessTwoID;//目标位置ID
    }
    /// <summary>
    /// 悔棋方法，悔棋按钮注册监听该方法。
    /// </summary>
    public void ResetChess()
    {
        //关闭相关的提示UI
        gameManager.HideLastPositionUI();
        gameManager.HideClickUI();
        gameManager.ClearCurrentCanEatUIStack();
        gameManager.ClearCurrentCanMoveUIStack();
        if (gameManager.chessPeople == 1) //单机模式
        {
            if(resetCount<1)//数量不足
                return;
            if (resetCount == 1) //奇数步
            {
                ResetOneChess();
            }
            else
            {
                ResetOneChess();
                ResetOneChess();
            }
            gameManager.checkmate.JudgeIfCheckmate();//判断是否将军
            gameManager.gameOver = false;//悔棋后不会处于结束状态
        }
        else if (gameManager.chessPeople == 2 || gameManager.chessPeople == 3) //单机或联网
        {
            ResetOneChess();
            gameManager.checkmate.JudgeIfCheckmate();
            gameManager.gameOver = false;
        }
    }

    /// <summary>
    /// 进行一次回退操作
    /// </summary>
    private void ResetOneChess()
    {
        if (resetCount <= 0) // 没有步数可以悔棋
            return;
        int f = resetCount - 1;//获取最后一步存放索引位置，resetCount默认指向一个空的待插入位置
                               // 获取存放的信息
        int oneID = chessSteps[f].chessOneID;
        int twoID = chessSteps[f].chessTwoID;
        GameObject gridOne, gridTwo, chessOne, chessTwo;
        gridOne = chessSteps[f].gridOne;
        gridTwo = chessSteps[f].gridTwo;
        chessOne = chessSteps[f].chessOne;
        chessTwo = chessSteps[f].chessTwo;
        //吃子，两颗棋子放回原来的位置还原棋盘状况
        if (chessTwo != null)
        {
            chessOne.transform.SetParent(gridOne.transform);
            chessOne.transform.localPosition = Vector3.zero;
            chessTwo.transform.SetParent(gridTwo.transform);
            chessTwo.transform.localPosition = Vector3.zero;
            gameManager.chessBoard[chessSteps[f].from.x, chessSteps[f].from.y] = oneID;
            gameManager.chessBoard[chessSteps[f].to.x, chessSteps[f].to.y] = twoID;
        }
        //移动，将移动的棋子还原
        else
        {
            chessOne.transform.SetParent(gridOne.transform);
            chessOne.transform.localPosition = Vector3.zero;
            gameManager.chessBoard[chessSteps[f].from.x, chessSteps[f].from.y] = oneID;
            gameManager.chessBoard[chessSteps[f].to.x, chessSteps[f].to.y] = 0;
        }
        // 黑方轮次，此时为红方悔棋
        if (gameManager.redChessMove == false)
        {
            UIManager.Instance.ShowTip("红方走");
            gameManager.redChessMove = true;
        }
        // 红方轮次，此时为黑方悔棋
        else
        {
            UIManager.Instance.ShowTip("黑方走");
            gameManager.redChessMove = false;
        }
        // 使用空对象替代，消除脏数据
        chessSteps[f] = new ChessStep();
        --resetCount;//记录步数-1
    }

    /// <summary>
    /// 记录下棋步数情况
    /// </summary>
    /// <param name="resetStepNum">当前步数存放索引</param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    /// <param name="toX"></param>
    /// <param name="toY"></param>
    /// <param name="ID1">对应悔棋那一步的第一个棋子ID</param>
    /// <param name="ID2">对应悔棋那一步的第二个ID</param>
    public void AddChess(int resetStepNum,int fromX,int fromY,int toX,int toY)
    {
        //当前需要记录的这步棋的数据存chess的结构体里，然后存进结构体数组里
        GameObject item1 = gameManager.boardGrid[fromX, fromY];//获取格子
        GameObject item2 = gameManager.boardGrid[toX, toY];
        chessSteps[resetStepNum].from.x = fromX;//记录起始和目标位置索引
        chessSteps[resetStepNum].from.y = fromY;
        chessSteps[resetStepNum].to.x = toX;
        chessSteps[resetStepNum].to.y = toY;
        chessSteps[resetStepNum].gridOne = item1;//记录格子
        chessSteps[resetStepNum].gridTwo = item2;
        //获取棋子前，先关闭多余的显示UI，方便获取子对象
        gameManager.ClearCurrentCanEatUIStack();
        gameManager.ClearCurrentCanMoveUIStack();
        gameManager.HideClickUI();
        gameManager.HideLastPositionUI();
        //获取存放在格子下的子对象棋子
        GameObject firstChess = item1.transform.GetChild(0).gameObject;
        chessSteps[resetStepNum].chessOne = firstChess;
        //查看棋盘情况记录棋子ID
        chessSteps[resetStepNum].chessOneID = gameManager.chessBoard[fromX,fromY];
        chessSteps[resetStepNum].chessTwoID = gameManager.chessBoard[toX,toY];
        //可能第二个棋子为空
        //有子，吃子情况
        if (item2.transform.childCount != 0)
        {
            GameObject secondChess = item2.transform.GetChild(0).gameObject;
            chessSteps[resetStepNum].chessTwo = secondChess;
        }
        resetCount++;
    }

}
