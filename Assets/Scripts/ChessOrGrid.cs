using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
/// <summary>
/// 棋子或格子挂载相同的脚本
/// 对于每次点击判断是格子还是棋子，随后执行相应的脚本，调用管理类处理UI和游戏逻辑判断
/// </summary>
public class ChessOrGrid : MonoBehaviour
{
    //格子索引
    public int xIndex, yIndex;
    //红棋或黑棋
    public bool isRed;
    //是否是格子
    public bool isGrid;
    //游戏管理引用
    private GameManager gameManager;
    //当前是棋子时，之后移动的时候需要设置的父对象格子
    private GameObject gridGo;
    // Start is called before the first frame update
    void Start()
    {
        gameManager=GameManager.Instance;//单例实例化
        gridGo = gameObject;//获取该游戏实体
    }
    /// <summary>
    /// 点击棋子或格子时触发的检测方法
    /// </summary>
    public void ClickCheck()
    {
        if (gameManager.gameOver) //游戏结束，直接返回
        {
            Debug.Log("游戏结束，点击无效");
            return;
        }
        int itemColorId;// 格子0 黑色1 红色2
        if (isGrid)
        {
            itemColorId = 0;
        }
        else
        {
            gridGo = transform.parent.gameObject;//得到父容器格子
            ChessOrGrid chessOrGrid = gridGo.GetComponent<ChessOrGrid>();//棋子本身不存索引,从父容器Grid中获取
            xIndex = chessOrGrid.xIndex;
            yIndex = chessOrGrid.yIndex;
            if (isRed)
            {
                itemColorId = 2;
            }
            else
            {
                itemColorId = 1;
            }
        }
        GridOrChessBehavior(itemColorId,xIndex,yIndex);
    }
    /// <summary>
    /// 格子和棋子的行为逻辑
    /// </summary>
    /// <param name="itemColorId">格子颜色</param>
    /// <param name="x">当前格子的x索引</param>
    /// <param name="y">当前格子的y索引</param>
    private void GridOrChessBehavior(int itemColorId,int x,int y)
    {
        int fromX, fromY, toX, toY;
        gameManager.ClearCurrentCanEatUIStack();
        gameManager.ClearCurrentCanMoveUIStack();
        switch (itemColorId)
        {
            //空格子
            case 0:
                toX = x;
                toY = y;
                //第一次点到空格子或连续两次点击空格子
                if (gameManager.lastChessOrGrid == null)
                {
                    Debug.Log("第一次空格子");
                    gameManager.lastChessOrGrid = this;
                    return;
                }

                if (gameManager.redChessMove) //红方轮次
                {
                    if (gameManager.lastChessOrGrid.isGrid)
                    {
                        return;
                    }
                    if (!gameManager.lastChessOrGrid.isRed)//上一次选中是否为黑色
                    {
                        gameManager.lastChessOrGrid = null;
                        return;
                    }
                    // 屏蔽客户端（黑方）移动红色棋子
                    if (gameManager.chessPeople == 3 && !gameManager.isServer)
                    {
                        gameManager.lastChessOrGrid = null;
                        return;
                    }
                    fromX = gameManager.lastChessOrGrid.xIndex;
                    fromY = gameManager.lastChessOrGrid.yIndex;
                    bool canMove = gameManager.rules.IsValidMove(gameManager.chessBoard, fromX, fromY, toX, toY);
                    if (!canMove)//非法，不移动，LastChessOrGrid保持不便
                    {
                        Debug.Log("空格子非法移动");
                        return;
                    }
                    //记录该步
                    gameManager.chessResting.AddChess(gameManager.chessResting.resetCount, fromX, fromY, toX, toY);
                    // 移动棋子
                    gameManager.movingOfChess.IsMove(gameManager.lastChessOrGrid.gameObject, gridGo, fromX, fromY, toX, toY);
                    UIManager.Instance.ShowTip("黑方走");
                    gameManager.checkmate.JudgeIfCheckmate();
                    gameManager.redChessMove = false;
                    gameManager.lastChessOrGrid = this;
                    gameManager.HideClickUI();
                    if (gameManager.chessPeople == 3) //联网模式，发送当前着法
                    {
                        gameManager.gameServer.SendMsg(new int[]{0,1,fromX,fromY,toX,toY});
                        return;
                    }
                    if (gameManager.gameOver)//游戏结束，AI不需要下棋
                    {
                        return;
                    }
                    if (gameManager.chessPeople == 2)//PVP模式，AI不需要下棋
                    {
                        return;
                    }
                    if (!gameManager.redChessMove)//黑棋移动轮次
                    {
                        //AI下棋
                        StartCoroutine("Robot");
                    }
                }
                else//黑方轮次
                {
                    if (gameManager.lastChessOrGrid.isGrid)
                    {
                        return;
                    }
                    if (gameManager.lastChessOrGrid.isRed)
                    {
                        gameManager.lastChessOrGrid = null;
                        return;
                    }
                    // 屏蔽服务器（红方）移动黑色棋子
                    if (gameManager.chessPeople == 3 && gameManager.isServer)
                    {
                        gameManager.lastChessOrGrid = null;
                        return;
                    }
                    fromX = gameManager.lastChessOrGrid.xIndex;
                    fromY = gameManager.lastChessOrGrid.yIndex;
                    bool canMove = gameManager.rules.IsValidMove(gameManager.chessBoard, fromX, fromY, toX, toY);
                    if (!canMove)//非法，不移动，LastChessOrGrid保持不便
                    {
                        Debug.Log("空格子非法移动");
                        return;
                    }
                    //记录该步
                    gameManager.chessResting.AddChess(gameManager.chessResting.resetCount, fromX, fromY, toX, toY);
                    // 移动棋子
                    gameManager.movingOfChess.IsMove(gameManager.lastChessOrGrid.gameObject, gridGo, fromX, fromY, toX, toY);
                    UIManager.Instance.ShowTip("红方走");
                    gameManager.checkmate.JudgeIfCheckmate();
                    gameManager.redChessMove = true;
                    gameManager.lastChessOrGrid = this;
                    gameManager.HideClickUI();
                    // 无论是否将死都不需要告诉对方，让对方结束到继续在本地判断是否结束即可
                    if (gameManager.chessPeople == 3) //联网模式，发送当前着法
                    {
                        gameManager.gameClient.SendMsg(new int[] { 0, 0, fromX, fromY, toX, toY });
                    }
                }
                break;
            //黑色棋子
            case 1:
                //黑色轮次，即选中新的棋子,更新UI显示
                if (!gameManager.redChessMove) 
                {
                    // 屏蔽服务器（红方）选中黑棋显示UI
                    if (gameManager.chessPeople == 3 && gameManager.isServer)
                    {
                        gameManager.lastChessOrGrid=null;
                        return;
                    }
                    fromX = x;
                    fromY = y;
                    //显示所有可移动的路径
                    gameManager.movingOfChess.ClickChess(fromX,fromY);
                    gameManager.lastChessOrGrid = this;
                    gameManager.ShowClickUI(transform);
                }
                else//红色轮次
                {
                    //红色棋子将要吃黑色棋子
                    if (gameManager.lastChessOrGrid == null)//上次未选中无效
                    {
                        //不用更新Last，因为红方无法选中黑方
                        return;
                    }
                    if (!gameManager.lastChessOrGrid.isRed)//上次选中黑色,连续2次黑色无效
                    {
                        //更新最新选中黑色
                        gameManager.lastChessOrGrid = this;
                        return;
                    }
                    // 屏蔽客户端（黑方）红吃黑
                    if (gameManager.chessPeople == 3 && !gameManager.isServer)
                    {
                        gameManager.lastChessOrGrid = null;
                        return;
                    }
                    fromX = gameManager.lastChessOrGrid.xIndex;
                    fromY = gameManager.lastChessOrGrid.yIndex;
                    toX = x;
                    toY = y;
                    bool canMove = gameManager.rules.IsValidMove(gameManager.chessBoard, fromX, fromY, toX, toY);
                    if (!canMove)
                    {
                        Debug.Log("黑色棋子红色轮次非法移动");
                        return;
                    }
                    gameManager.chessResting.AddChess(gameManager.chessResting.resetCount, fromX, fromY, toX, toY);
                    gameManager.movingOfChess.IsEat(gameManager.lastChessOrGrid.gameObject,gameObject,fromX,fromY,toX,toY);
                    gameManager.redChessMove = false;
                    UIManager.Instance.ShowTip("黑方走");
                    gameManager.lastChessOrGrid = null;
                    gameManager.checkmate.JudgeIfCheckmate();
                    gameManager.HideClickUI();
                    if (gameManager.chessPeople == 3) //联网模式，发送当前着法
                    {
                        gameManager.gameServer.SendMsg(new int[] { 0, 1, fromX, fromY, toX, toY });
                        return;
                    }
                    // 玩家移动完成后判断当前模式和轮次决定是否执行AI
                    if (gameManager.gameOver)//游戏结束
                    {
                        return;
                    }

                    if (gameManager.chessPeople == 2) //PVP模式
                    {
                        return;
                    }
                    if (!gameManager.redChessMove) //黑色轮次
                    {
                        //启动协程AI下棋
                        StartCoroutine("Robot");
                    }
                }
                break;
            
            //红色棋子
            case 2:
                //红色轮次，即选中新的棋子，更新UI显示
                if (gameManager.redChessMove)
                {
                    // 屏蔽客户端选中红旗显示UI
                    if (gameManager.chessPeople == 3 && !gameManager.isServer)
                    {
                        gameManager.lastChessOrGrid=null;
                        return;
                    }
                    fromX = x;
                    fromY = y;
                    //显示所有可能的路径
                    gameManager.movingOfChess.ClickChess(fromX,fromY);
                    gameManager.lastChessOrGrid = this;
                    gameManager.ShowClickUI(transform);
                }
                else//黑色轮次
                {
                    // 上次未选中，黑方轮次不能选中红色棋子，不用更新
                    if (gameManager.lastChessOrGrid == null)
                    {
                        return;
                    }
                    // 连续两次红色，更新最后选中
                    if (gameManager.lastChessOrGrid.isRed)
                    {
                        gameManager.lastChessOrGrid = this;
                        return;
                    }
                    // 屏蔽服务器（红方）黑吃红
                    if (gameManager.chessPeople == 3 && gameManager.isServer)
                    {
                        gameManager.lastChessOrGrid = null;
                        return;
                    }
                    fromX = gameManager.lastChessOrGrid.xIndex;
                    fromY = gameManager.lastChessOrGrid.yIndex;
                    toX = x;
                    toY = y;
                    bool canMove = gameManager.rules.IsValidMove(gameManager.chessBoard, fromX, fromY, toX, toY);
                    if (!canMove)
                    {
                        Debug.Log("红色棋子黑色轮次非法移动");
                        return;
                    }
                    gameManager.chessResting.AddChess(gameManager.chessResting.resetCount,fromX,fromY,toX,toY);
                    gameManager.movingOfChess.IsEat(gameManager.lastChessOrGrid.gameObject,gameObject,fromX,fromY,toX,toY);
                    gameManager.redChessMove = true;
                    gameManager.lastChessOrGrid = null;
                    UIManager.Instance.ShowTip("红方走");
                    gameManager.checkmate.JudgeIfCheckmate();
                    gameManager.HideClickUI();
                    if (gameManager.chessPeople == 3) //联网模式，发送当前着法
                    {
                        gameManager.gameClient.SendMsg(new int[] { 0, 0, fromX, fromY, toX, toY });
                    }
                }
                break;
            default:
                break;
        }
    }
    /// <summary>
    /// AI下棋的协程
    /// </summary>
    /// <returns></returns>
    IEnumerator Robot()
    {
        UIManager.Instance.ShowTip("对方正在思考");
        yield return new WaitForSeconds(0.01f);
        RobotMove();
    }
    /// <summary>
    /// AI下棋的方法
    /// </summary>
    private void RobotMove()
    {
        gameManager.movingOfChess.HaveAGoodMove(
            gameManager.searchEngine.SearchAGoodMove(gameManager.chessBoard));
        //更新轮次信息
        gameManager.redChessMove = true;
        UIManager.Instance.ShowTip("红方走");
        gameManager.checkmate.JudgeIfCheckmate();
    }
}
