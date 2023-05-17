using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// 棋子的移动类
/// </summary>
public class MovingOfChess
{
    private GameManager gameManager;
    //当前深度下的着法编号计数器
    private int moveCount;

    //存放所有合法的着法列表，第一个索引为搜索深度，第二个索引代表当前深度下的着法编号
    public ChessReseting.ChessStep[,] moveList = new ChessReseting.ChessStep[8, 80];
    public MovingOfChess(GameManager mGameManager)
    {
        gameManager = mGameManager;
    }
    /// <summary>
    /// 棋子的移动方法
    /// </summary>
    /// <param name="chessGo">要移动的棋子游戏物体</param>
    /// <param name="targetGrid">要移动的格子游戏物体</param>
    /// <param name="fromX">起始X位置</param>
    /// <param name="fromY">起始Y位置</param>
    /// <param name="toX">目的X位置</param>
    /// <param name="toY">目的Y位置</param>
    public void IsMove(GameObject chessGo, GameObject targetGrid, int fromX, int fromY, int toX, int toY)
    {
        //Debug.Log("IsMove");
        AudioSourceManager.Instance.PlaySound(1);
        gameManager.ShowLastPositionUI(chessGo.transform.position);//显示Last UI
        // 更新chessGo的父对象为targetGrid，即将棋子chessGo放入格子targetGrid
        chessGo.transform.SetParent(targetGrid.transform);
        chessGo.transform.localPosition = Vector3.zero;
        // 更新棋盘状况
        gameManager.chessBoard[toX, toY] = gameManager.chessBoard[fromX, fromY];
        gameManager.chessBoard[fromX, fromY] = 0;
    }

    /// <summary>
    /// 棋子吃子的方法
    /// </summary>
    /// <param name="firstChess">将要移动的棋子</param>
    /// <param name="secondChess">将要吃掉的棋子</param>
    /// <param name="x1">移动棋子的X位置</param>
    /// <param name="y1">移动棋子的Y位置</param>
    /// <param name="x2">吃掉棋子的X位置</param>
    /// <param name="y2">吃掉棋子的Y位置</param>
    public void IsEat(GameObject firstChess, GameObject secondChess, int x1, int y1, int x2, int y2)
    {
        //Debug.Log("IsEat");
        AudioSourceManager.Instance.PlaySound(2);
        gameManager.ShowLastPositionUI(firstChess.transform.position);//显示Last UI
        GameObject secondChessGrid = secondChess.transform.parent.gameObject;//获取目标位置的格子
        firstChess.transform.SetParent(secondChessGrid.transform);//将该棋子放置到目标格子
        firstChess.transform.localPosition = Vector3.zero;
        //更新棋盘状况
        gameManager.chessBoard[x2, y2] = gameManager.chessBoard[x1, y1];
        gameManager.chessBoard[x1, y1] = 0;
        //被吃棋子扔回池子
        gameManager.BeEat(secondChess);
    }
    /// <summary>
    /// 判断当前点击到的是什么类型的棋子从而执行相应方法
    /// </summary>
    /// <param name="fromX">选中的棋子X位置</param>
    /// <param name="fromY">选中的棋子Y位置</param>
    public void ClickChess(int fromX, int fromY)
    {
        int chessID = gameManager.chessBoard[fromX, fromY];
        switch (chessID)
        {
            case 1://黑将
                GetJiangMove(gameManager.chessBoard, fromX, fromY);
                break;
            case 8://红帅
                GetShuaiMove(gameManager.chessBoard, fromX, fromY);
                break;
            case 2://黑车
            case 9:
                GetJuMove(gameManager.chessBoard, fromX, fromY);
                break;
            case 3://黑马
            case 10:
                GetMaMove(gameManager.chessBoard, fromX, fromY);
                break;
            case 4://黑炮
            case 11:
                GetPaoMove(gameManager.chessBoard, fromX, fromY);
                break;
            case 5://黑士
                GetB_ShiMove(gameManager.chessBoard, fromX, fromY);
                break;
            case 12:
                GetR_ShiMove(gameManager.chessBoard, fromX, fromY);
                break;
            case 6://黑象
            case 13:
                GetXiangMove(gameManager.chessBoard, fromX, fromY);
                break;
            case 7://黑卒
                GetB_BingMove(gameManager.chessBoard, fromX, fromY);
                break;
            case 14:
                GetR_BingMove(gameManager.chessBoard, fromX, fromY);
                break;
            default:
                break;

        }
    }

    #region 获取每种棋子当前可以移动的所有位置传递显示
    /// <summary>
    /// 将帅
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetJiangMove(int[,] position, int fromX, int fromY)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                //判断位置是否合法
                if (gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
                    GetCanMovePos(position, fromX, fromY, x, y);//显示CanMove UI
            }
        }
    }

    private void GetShuaiMove(int[,] position, int fromX, int fromY)
    {
        for (int x = 7; x < 10; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                if (gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
                    GetCanMovePos(position, fromX, fromY, x, y);
            }
        }
    }
    /// <summary>
    /// 黑俥红车
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetJuMove(int[,] position, int fromX, int fromY)
    {
        int x, y;
        //得到当前选中棋子的ID，目的是为了遍历时判断第一个不为空格子的棋子是否为同一边
        int chessID = position[fromX, fromY];
        //上
        x = fromX - 1;
        y = fromY;
        while (x >= 0)
        {
            if (position[x, y] == 0)
            {
                GetCanMovePos(position, fromX, fromY, x, y);
            }
            else
            {
                if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                {
                    GetCanMovePos(position, fromX, fromY, x, y);
                }
                break;
            }
            x--;
        }
        //下
        x = fromX + 1;
        y = fromY;
        while (x < 10)
        {
            if (position[x, y] == 0)
            {
                GetCanMovePos(position, fromX, fromY, x, y);
            }
            else
            {
                if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                {
                    GetCanMovePos(position, fromX, fromY, x, y);
                }
                break;
            }
            x++;
        }
        //左
        x = fromX;
        y = fromY - 1;
        while (y >= 0)
        {
            if (position[x, y] == 0)
            {
                GetCanMovePos(position, fromX, fromY, x, y);
            }
            else
            {
                if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                {
                    GetCanMovePos(position, fromX, fromY, x, y);
                }
                break;
            }
            y--;
        }
        //右
        x = fromX;
        y = fromY + 1;
        while (y < 9)
        {
            if (position[x, y] == 0)
            {
                GetCanMovePos(position, fromX, fromY, x, y);
            }
            else
            {
                if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                {
                    GetCanMovePos(position, fromX, fromY, x, y);
                }
                break;
            }
            y++;
        }
    }
    /// <summary>
    /// 红黑马
    /// </summary>
    /// <param name="position">棋盘数据</param>
    /// <param name="fromX">起始x坐标</param>
    /// <param name="fromY">起始y坐标</param>
    private void GetMaMove(int[,] position, int fromX, int fromY)
    {
        int x, y;
        //竖日
        //左上
        x = fromX - 2;
        y = fromY - 1;
        if (x >= 0 && y >= 0 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }
        //左下
        x = fromX + 2;
        y = fromY - 1;
        if (x < 10 && y >= 0 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }
        //右上
        x = fromX - 2;
        y = fromY + 1;
        if (x >= 0 && y < 9 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }
        //右下
        x = fromX + 2;
        y = fromY + 1;
        if (x < 10 && y < 9 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }
        //横日
        //左上
        x = fromX - 1;
        y = fromY - 2;
        if (x >= 0 && y >= 0 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }
        //左下
        x = fromX + 1;
        y = fromY - 2;
        if (x < 10 && y >= 0 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }
        //右上
        x = fromX - 1;
        y = fromY + 2;
        if (x >= 0 && y < 9 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }
        //右下
        x = fromX + 1;
        y = fromY + 2;
        if (x < 10 && y < 9 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }
    }
    /// <summary>
    /// 红黑炮
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetPaoMove(int[,] position, int fromX, int fromY)
    {
        int x, y;
        bool flag;//是否满足翻山条件
        int chessID = position[fromX, fromY];
        //上
        x = fromX - 1;
        y = fromY;
        flag = false;
        while (x >= 0)
        {
            if (position[x, y] == 0)//空格子
            {
                if (!flag)
                {
                    GetCanMovePos(position, fromX, fromY, x, y);//未达成翻山条件可显示
                }
            }
            else
            {
                if (!flag)//经过棋子时，开启翻山条件
                {
                    flag = true;
                }
                //已经翻山了，判断是否同一方
                //同方，则结束遍历
                //不同方，可以吃子，同时结束遍历
                else
                {
                    if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                    {
                        GetCanMovePos(position, fromX, fromY, x, y);
                    }
                    break;
                }
            }
            x--;
        }
        //下
        x = fromX + 1;
        flag = false;
        while (x < 10)
        {
            if (position[x, y] == 0)
            {
                if (!flag)
                {
                    GetCanMovePos(position, fromX, fromY, x, y);
                }
            }
            else
            {
                if (!flag)
                {
                    flag = true;
                }
                else
                {
                    if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                    {
                        GetCanMovePos(position, fromX, fromY, x, y);
                    }
                    break;
                }
            }
            x++;
        }
        //左
        x = fromX;
        y = fromY - 1;
        flag = false;
        while (y >= 0)
        {
            if (position[x, y] == 0)
            {
                if (!flag)
                {
                    GetCanMovePos(position, fromX, fromY, x, y);
                }
            }
            else
            {
                if (!flag)
                {
                    flag = true;
                }
                else
                {
                    if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                    {
                        GetCanMovePos(position, fromX, fromY, x, y);
                    }
                    break;
                }
            }
            y--;
        }

        //右
        y = fromY + 1;
        flag = false;
        while (y < 9)
        {
            if (position[x, y] == 0)
            {
                if (!flag)
                {
                    GetCanMovePos(position, fromX, fromY, x, y);
                }
            }
            else
            {
                if (!flag)
                {
                    flag = true;
                }
                else
                {
                    if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                    {
                        GetCanMovePos(position, fromX, fromY, x, y);
                    }
                    break;
                }
            }
            y++;
        }
    }
    /// <summary>
    /// 黑士
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetB_ShiMove(int[,] position, int fromX, int fromY)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                if (gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
                    GetCanMovePos(position, fromX, fromY, x, y);
            }
        }
    }
    /// <summary>
    /// 红仕
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetR_ShiMove(int[,] position, int fromX, int fromY)
    {
        for (int x = 7; x < 10; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                if (gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
                    GetCanMovePos(position, fromX, fromY, x, y);
            }
        }
    }
    /// <summary>
    /// 红相黑象
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetXiangMove(int[,] position, int fromX, int fromY)
    {
        int x, y;
        //左上
        x = fromX - 2;
        y = fromY - 2;
        if (x >= 0 && y >= 0 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }
        //左下
        x = fromX + 2;
        y = fromY - 2;
        if (x < 10 && y >= 0 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }
        //右上
        x = fromX - 2;
        y = fromY + 2;
        if (x >= 0 && y < 9 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }
        //右下
        x = fromX + 2;
        y = fromY + 2;
        if (x < 10 && y < 9 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }
    }

    private void GetB_BingMove(int[,] position, int fromX, int fromY)
    {
        int x, y;
        int chessID = position[fromX, fromY];
        x = fromX + 1;
        y = fromY;
        if (x < 10 && !gameManager.rules.IsSameSide(chessID, position[x, y]))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }

        if (fromX > 4)
        {
            x = fromX;
            //左边
            y = fromY - 1;
            if (y >= 0 && !gameManager.rules.IsSameSide(chessID, position[x, y]))
            {
                GetCanMovePos(position, fromX, fromY, x, y);
            }
            //右边
            y = fromY + 1;
            if (y < 9 && !gameManager.rules.IsSameSide(chessID, position[x, y]))
            {
                GetCanMovePos(position, fromX, fromY, x, y);
            }
        }
    }

    private void GetR_BingMove(int[,] position, int fromX, int fromY)
    {
        int x, y;
        int chessID = position[fromX, fromY];
        x = fromX - 1;//向上移动
        y = fromY;
        if (x >= 0 && !gameManager.rules.IsSameSide(chessID, position[x, y]))
        {
            GetCanMovePos(position, fromX, fromY, x, y);
        }

        if (fromX < 5)
        {
            //左边
            x = fromX;
            y = fromY - 1;
            if (y >= 0 && !gameManager.rules.IsSameSide(chessID, position[x, y]))
            {
                GetCanMovePos(position, fromX, fromY, x, y);
            }
            //右边
            y = fromY + 1;
            if (y < 9 && !gameManager.rules.IsSameSide(chessID, position[x, y]))
            {
                GetCanMovePos(position, fromX, fromY, x, y);
            }
        }
    }
    #endregion

    /// <summary>
    /// 把传递的移动路径中合法的显示出来
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    /// <param name="toX"></param>
    /// <param name="toY"></param>
    private void GetCanMovePos(int[,] position, int fromX, int fromY, int toX, int toY)
    {
        // 判断是否构成KingKill，即移动后造成将帅碰面
        if (gameManager.rules.KingKill(position, fromX, fromY, toX, toY))
        {
            //Debug.Log("构成KingKill，不可移动");
            return;
        }

        GameObject item;//获取UI object
        if (position[toX, toY] == 0) //获取CanMoveUI
        {
            item = gameManager.PopCanMoveUI();
        }
        else//有棋子获取CanEatUI
        {
            item = gameManager.PopCanEatUI();
        }
        //将UI在目标位置显示
        item.transform.SetParent(gameManager.boardGrid[toX, toY].transform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// AI具体走起方法，将搜索引擎计算出的着法执行
    /// </summary>
    public void HaveAGoodMove(ChessReseting.ChessStep aChessStepStep)
    {
        //记录该步
        gameManager.chessResting.AddChess(gameManager.chessResting.resetCount,
            aChessStepStep.gridOne.GetComponent<ChessOrGrid>().xIndex,
            aChessStepStep.gridOne.GetComponent<ChessOrGrid>().yIndex,
            aChessStepStep.gridTwo.GetComponent<ChessOrGrid>().xIndex,
            aChessStepStep.gridTwo.GetComponent<ChessOrGrid>().yIndex);
        //移动该步
        if (aChessStepStep.chessTwo == null) //移动棋子
        {
            IsMove(aChessStepStep.chessOne, aChessStepStep.gridTwo,
                aChessStepStep.gridOne.GetComponent<ChessOrGrid>().xIndex,
                aChessStepStep.gridOne.GetComponent<ChessOrGrid>().yIndex,
                aChessStepStep.gridTwo.GetComponent<ChessOrGrid>().xIndex,
                aChessStepStep.gridTwo.GetComponent<ChessOrGrid>().yIndex);
        }
        else//吃子
        {
            IsEat(aChessStepStep.chessOne, aChessStepStep.chessTwo,
            aChessStepStep.gridOne.GetComponent<ChessOrGrid>().xIndex,
            aChessStepStep.gridOne.GetComponent<ChessOrGrid>().yIndex,
            aChessStepStep.gridTwo.GetComponent<ChessOrGrid>().xIndex,
            aChessStepStep.gridTwo.GetComponent<ChessOrGrid>().yIndex);
        }
    }

    /// <summary>
    /// 产生当前局面所有棋子可能移动着法的方法
    /// </summary>
    /// <param name="position">当前棋盘状况</param>
    /// <param name="depth">当前搜索深度</param>
    /// <param name="isOdd">true为奇数AI层，false为偶数玩家层</param>
    /// <returns></returns>
    public int CreatePossibleMove(int[,] position,int depth,bool isOdd)
    {
        int chessID;
        moveCount = 0;
        for (int x = 0; x < 10; ++x)
        {
            for (int y = 0; y < 9; y++)
            {
                if (position[x, y] != 0)
                {
                    chessID = position[x, y];
                    if (isOdd&&gameManager.rules.IsRed(chessID))
                    {
                        //奇数层，产生AI黑棋走法,跳过红旗
                        continue;
                    }

                    if (!isOdd && gameManager.rules.IsBlack(chessID))
                    {
                        //偶数层，产生红旗走法，跳过黑棋
                        continue;
                    }
                    switch (chessID)
                    {
                        case 1://黑将
                            GetJiangMove(position, x, y,depth);
                            break;
                        case 8://红帅
                            GetShuaiMove(position, x, y, depth);
                            break;
                        case 2://黑车
                        case 9:
                            GetJuMove(position, x, y, depth);
                            break;
                        case 3://黑马
                        case 10:
                            GetMaMove(position, x, y, depth);
                            break;
                        case 4://黑炮
                        case 11:
                            GetPaoMove(position, x, y, depth);
                            break;
                        case 5://黑士
                            GetB_ShiMove(position, x, y, depth);
                            break;
                        case 12:
                            GetR_ShiMove(position, x, y, depth);
                            break;
                        case 6://黑象
                        case 13:
                            GetXiangMove(position, x, y, depth);
                            break;
                        case 7://黑卒
                            GetB_BingMove(position, x, y, depth);
                            break;
                        case 14:
                            GetR_BingMove(position, x, y, depth);
                            break;
                        default:
                            break;

                    }
                }

            }
        }
        return moveCount;
    }

    private void AddMove(int[,] position, int fromX, int fromY, int toX, int toY, int depth)
    {
        // 该步棋不够成KingKill
        if (!gameManager.rules.KingKill(position, fromX, fromY, toX, toY))
        {
            //该着法加入
            moveList[depth, moveCount].from.x = fromX;
            moveList[depth, moveCount].from.y = fromY;
            moveList[depth, moveCount].to.x = toX;
            moveList[depth, moveCount].to.y = toY;
            moveCount++;

        }
    }
    #region 获取每种棋子当前可以移动的所有位置添加到着法数组
    /// <summary>
    /// 将帅
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetJiangMove(int[,] position, int fromX, int fromY,int depth)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                //判断位置是否合法
                if (gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
                    AddMove(position,fromX,fromY,x,y,depth);
            }
        }
    }

    private void GetShuaiMove(int[,] position, int fromX, int fromY,int depth)
    {
        for (int x = 7; x < 10; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                if (gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
                    AddMove(position, fromX, fromY, x, y, depth);
            }
        }
    }
    /// <summary>
    /// 黑俥红车
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetJuMove(int[,] position, int fromX, int fromY,int depth)
    {
        int x, y;
        //得到当前选中棋子的ID，目的是为了遍历时判断第一个不为空格子的棋子是否为同一边
        int chessID = position[fromX, fromY];
        //上
        x = fromX - 1;
        y = fromY;
        while (x >= 0)
        {
            if (position[x, y] == 0)
            {
                AddMove(position,fromX,fromY,x,y,depth);
            }
            else
            {
                if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                {
                    AddMove(position,fromX,fromY,x,y,depth);
                }
                break;
            }
            x--;
        }
        //下
        x = fromX + 1;
        y = fromY;
        while (x < 10)
        {
            if (position[x, y] == 0)
            {
                AddMove(position,fromX,fromY,x,y,depth);
            }
            else
            {
                if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                {
                    AddMove(position,fromX,fromY,x,y,depth);
                }
                break;
            }
            x++;
        }
        //左
        x = fromX;
        y = fromY - 1;
        while (y >= 0)
        {
            if (position[x, y] == 0)
            {
                AddMove(position,fromX,fromY,x,y,depth);
            }
            else
            {
                if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                {
                    AddMove(position,fromX,fromY,x,y,depth);
                }
                break;
            }
            y--;
        }
        //右
        x = fromX;
        y = fromY + 1;
        while (y < 9)
        {
            if (position[x, y] == 0)
            {
                AddMove(position,fromX,fromY,x,y,depth);
            }
            else
            {
                if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                {
                    AddMove(position,fromX,fromY,x,y,depth);
                }
                break;
            }
            y++;
        }
    }
    /// <summary>
    /// 红黑马
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetMaMove(int[,] position, int fromX, int fromY,int depth)
    {
        int x, y;
        //竖日
        //左上
        x = fromX - 2;
        y = fromY - 1;
        if (x >= 0 && y >= 0 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }
        //左下
        x = fromX + 2;
        y = fromY - 1;
        if (x < 10 && y >= 0 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }
        //右上
        x = fromX - 2;
        y = fromY + 1;
        if (x >= 0 && y < 9 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }
        //右下
        x = fromX + 2;
        y = fromY + 1;
        if (x < 10 && y < 9 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }
        //横日
        //左上
        x = fromX - 1;
        y = fromY - 2;
        if (x >= 0 && y >= 0 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }
        //左下
        x = fromX + 1;
        y = fromY - 2;
        if (x < 10 && y >= 0 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }
        //右上
        x = fromX - 1;
        y = fromY + 2;
        if (x >= 0 && y < 9 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }
        //右下
        x = fromX + 1;
        y = fromY + 2;
        if (x < 10 && y < 9 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }
    }
    /// <summary>
    /// 红黑炮
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetPaoMove(int[,] position, int fromX, int fromY,int depth)
    {
        int x, y;
        bool flag;//是否满足翻山条件
        int chessID = position[fromX, fromY];
        //上
        x = fromX - 1;
        y = fromY;
        flag = false;
        while (x >= 0)
        {
            if (position[x, y] == 0)//空格子
            {
                if (!flag)
                {
                    AddMove(position,fromX,fromY,x,y,depth);//未达成翻山条件可显示
                }
            }
            else
            {
                if (!flag)//经过棋子时，开启翻山条件
                {
                    flag = true;
                }
                //已经翻山了，判断是否同一方
                //同方，则结束遍历
                //不同方，可以吃子，同时结束遍历
                else
                {
                    if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                    {
                        AddMove(position,fromX,fromY,x,y,depth);
                    }
                    break;
                }
            }
            x--;
        }
        //下
        x = fromX + 1;
        flag = false;
        while (x < 10)
        {
            if (position[x, y] == 0)
            {
                if (!flag)
                {
                    AddMove(position,fromX,fromY,x,y,depth);
                }
            }
            else
            {
                if (!flag)
                {
                    flag = true;
                }
                else
                {
                    if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                    {
                        AddMove(position,fromX,fromY,x,y,depth);
                    }
                    break;
                }
            }
            x++;
        }
        //左
        x = fromX;
        y = fromY - 1;
        flag = false;
        while (y >= 0)
        {
            if (position[x, y] == 0)
            {
                if (!flag)
                {
                    AddMove(position,fromX,fromY,x,y,depth);
                }
            }
            else
            {
                if (!flag)
                {
                    flag = true;
                }
                else
                {
                    if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                    {
                        AddMove(position,fromX,fromY,x,y,depth);
                    }
                    break;
                }
            }
            y--;
        }

        //右
        y = fromY + 1;
        flag = false;
        while (y < 9)
        {
            if (position[x, y] == 0)
            {
                if (!flag)
                {
                    AddMove(position,fromX,fromY,x,y,depth);
                }
            }
            else
            {
                if (!flag)
                {
                    flag = true;
                }
                else
                {
                    if (!gameManager.rules.IsSameSide(chessID, position[x, y]))
                    {
                        AddMove(position,fromX,fromY,x,y,depth);
                    }
                    break;
                }
            }
            y++;
        }
    }
    /// <summary>
    /// 黑士
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetB_ShiMove(int[,] position, int fromX, int fromY,int depth)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                if (gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
                    AddMove(position, fromX, fromY, x, y, depth);
            }
        }
    }
    /// <summary>
    /// 红仕
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetR_ShiMove(int[,] position, int fromX, int fromY,int depth)
    {
        for (int x = 7; x < 10; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                if (gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
                    AddMove(position, fromX, fromY, x, y, depth);

            }
        }
    }
    /// <summary>
    /// 红相黑象
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    private void GetXiangMove(int[,] position, int fromX, int fromY,int depth)
    {
        int x, y;
        //左上
        x = fromX - 2;
        y = fromY - 2;
        if (x >= 0 && y >= 0 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }
        //左下
        x = fromX + 2;
        y = fromY - 2;
        if (x < 10 && y >= 0 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }
        //右上
        x = fromX - 2;
        y = fromY + 2;
        if (x >= 0 && y < 9 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }
        //右下
        x = fromX + 2;
        y = fromY + 2;
        if (x < 10 && y < 9 && gameManager.rules.IsValidMove(position, fromX, fromY, x, y))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }
    }

    private void GetB_BingMove(int[,] position, int fromX, int fromY,int depth)
    {
        int x, y;
        int chessID = position[fromX, fromY];
        x = fromX + 1;
        y = fromY;
        if (x < 10 && !gameManager.rules.IsSameSide(chessID, position[x, y]))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }

        if (fromX > 4)
        {
            x = fromX;
            //左边
            y = fromY - 1;
            if (y >= 0 && !gameManager.rules.IsSameSide(chessID, position[x, y]))
            {
                AddMove(position,fromX,fromY,x,y,depth);
            }
            //右边
            y = fromY + 1;
            if (y < 9 && !gameManager.rules.IsSameSide(chessID, position[x, y]))
            {
                AddMove(position,fromX,fromY,x,y,depth);
            }
        }
    }

    private void GetR_BingMove(int[,] position, int fromX, int fromY,int depth)
    {
        int x, y;
        int chessID = position[fromX, fromY];
        x = fromX - 1;//向上移动
        y = fromY;
        if (x >= 0 && !gameManager.rules.IsSameSide(chessID, position[x, y]))
        {
            AddMove(position,fromX,fromY,x,y,depth);
        }

        if (fromX < 5)
        {
            //左边
            x = fromX;
            y = fromY - 1;
            if (y >= 0 && !gameManager.rules.IsSameSide(chessID, position[x, y]))
            {
                AddMove(position,fromX,fromY,x,y,depth);
            }
            //右边
            y = fromY + 1;
            if (y < 9 && !gameManager.rules.IsSameSide(chessID, position[x, y]))
            {
                AddMove(position,fromX,fromY,x,y,depth);
            }
        }
    }
    #endregion
}
