using System;
using UnityEngine;
using Random = System.Random;

public class SearchEngine
{
    public int searchDepth;//搜索深度，即步数
    private GameManager gameManager;
    private ChessReseting.ChessStep bestStep;//保存最佳着法
    private int[,] tempBoard = new int[10, 9];//临时棋盘，进行演算
    public const int INFINITY = 20000;//作为边界，如果有合法值在该边界内
    private ChessReseting.ChessIndex[] relatedPos = new ChessReseting.ChessIndex[20];//记录棋子的相关位置，最多如车也就17个位置
    private int posCount;//相关位置的索引

    // 棋子的基本价值
    public static readonly int[] baseValue = new int[15]
    {
        // ID: 1帅 2车 3马 4炮 5仕 6相 7兵
        0, 10000, 900, 500, 600, 250, 250, 100,
           10000, 900, 500, 600, 250, 250, 100,
    };
    //棋子的灵活性分数（每多一个可移动位置可以增加的分数）
    public static readonly int[] flexValue = new int[15]
    {
        // ID: 1帅 2车 3马 4炮 5仕 6相 7兵
        0, 0, 6, 12, 6, 2, 1, 15,
           0, 6, 12, 6, 2, 1, 15,
    };
    //红兵的位置附加值数组，结合基础值使用
    public static readonly int[,] r_bingValue = new int[10, 9]
    {
        {0, 0, 0, 0, 0, 0, 0, 0, 0},
        {90, 90, 110, 120, 120, 120, 110, 90, 90},
        {90, 90, 110, 120, 120, 120, 110, 90, 90},
        {70, 90, 110, 110, 110, 110, 110, 90, 70},
        {70, 70, 70, 70, 70, 70, 70, 70, 70},
        {0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 0},
    };
    //黑兵的位置附加值数组
    public static readonly int[,] b_bingValue = new int[10, 9]
    {
        {0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 0},
        {70, 70, 70, 70, 70, 70, 70, 70, 70},
        {70, 90, 110, 110, 110, 110, 110, 90, 70},
        {90, 90, 110, 120, 120, 120, 110, 90, 90},
        {90, 90, 110, 120, 120, 120, 110, 90, 90},
        {0, 0, 0, 0, 0, 0, 0, 0, 0},
    };
    //每个位置威胁值（处于敌方棋子攻击的范围则威胁值大）
    private int[,] attackPos;
    //每个位置保护值（处于友方棋子攻击的范围则保护值大）
    private int[,] guardPos;
    //每个位置灵活性分数
    private int[,] flexPos;
    //每个位置棋子的总价值
    private int[,] chessValue;
    //记录评估函数调用次数
    private int evaluateCallTime;
    public SearchEngine()
    {
        gameManager=GameManager.Instance;
        InitializeHashKey();
    }
    /// <summary>
    /// 搜索当前最佳着法
    /// </summary>
    /// <returns></returns>
    public ChessReseting.ChessStep SearchAGoodMove(int[,]position)
    {
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();//开始计时
        //设置搜索深度
        searchDepth = gameManager.currentLevel;
        //用于测试评估函数调用情况
        evaluateCallTime = 0;
        //拷贝临时棋盘
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                tempBoard[i, j] = position[i, j];
            }
        }
        //int testS = NegMax(searchDepth);
        //int testS = AlphaBeta(searchDepth, -INFINITY, INFINITY);
        //int testS = FAlphaBeta(searchDepth, -INFINITY, INFINITY);
        //int testS = AspirationSearch(searchDepth);
        //int testS = PrincipalVariation(searchDepth, -INFINITY, INFINITY);
        //历史启发+Alpha-Beta
        ResetHistoryTable();
        int testS = HistoryHeuristicAlphaBeta(searchDepth, -INFINITY, INFINITY);
        //置换表+Alpha-Beta
        //CalculateInitHashKey(tempBoard);
        //int testS = AlphaBeta_TT(searchDepth, -INFINITY, INFINITY);
        //置换表+历史启发表
        //CalculateInitHashKey(tempBoard);
        //ResetHistoryTable();
        //int testS = NegaScout_HH_TT(searchDepth, -INFINITY, INFINITY);
        watch.Stop();//停止计时
        Debug.Log("耗时:" + (watch.ElapsedMilliseconds));//输出时间 毫秒
        Debug.Log("本次搜索树调用评估函数次数:" + evaluateCallTime + " 最佳得分为：" + testS);
        //完成后需要将索引位置的格子和棋子获取填充到bestStep中
        HandleBestStep(position);
        return bestStep;
    }

    private void HandleBestStep(int[,] position)
    {
        GameObject grid1 = gameManager.boardGrid[bestStep.from.x, bestStep.from.y];
        GameObject grid2 = gameManager.boardGrid[bestStep.to.x, bestStep.to.y];
        Debug.Log(grid1.name+"-"+gameManager.chessBoard[bestStep.from.x, bestStep.from.y] +"->"+grid2.name+"-"+ gameManager.chessBoard[bestStep.to.x, bestStep.to.y]);
        bestStep.gridOne = grid1;
        bestStep.gridTwo = grid2;
        bestStep.chessOne = grid1.transform.GetChild(0).gameObject;
        if (grid2.transform.childCount != 0)
        {
            bestStep.chessTwo = grid2.transform.GetChild(0).gameObject;
        }
        bestStep.chessOneID = position[bestStep.from.x, bestStep.from.y];
        bestStep.chessTwoID = position[bestStep.to.x, bestStep.to.y];
    }
    /// <summary>
    /// 负极大值算法
    /// </summary>
    /// <param name="depth"></param>
    /// <returns></returns>
    private int NegMax(int depth)
    {
        //最佳得分
        int best = -INFINITY;
        //当前调用的得分
        int score;
        //当前局面下一步总共可以走的着法
        int count;
        // 改棋移动目标位置原来的ID值,用来还原，chessList仅保存了位置索引
        int targetChessID;
        int willKillKing = IsGameOver(tempBoard, depth);
        if (willKillKing != 0)
        {
            //Debug.Log("游戏结束，将帅被杀"+willKillKing);
            //棋局结束，将帅有阵亡
            return willKillKing;
        }
        //到达底层，直接返回评估值
        if (depth <= 0)
        {
            return Evaluate(tempBoard, IsEvenLayer(depth));
            
        }
        //获取所有可能的着法
        count = gameManager.movingOfChess.CreatePossibleMove(tempBoard,depth,IsOddLayer(depth));
        for (int i = 0; i < count; ++i)
        {
            targetChessID=MakeMove(gameManager.movingOfChess.moveList[depth,i]);
            score = -NegMax(depth - 1);
            UnMakeMove(gameManager.movingOfChess.moveList[depth,i],targetChessID);
            if (score > best)
            {
                best = score;
                if (depth == searchDepth)//在根节点时进行更新
                {
                    bestStep = gameManager.movingOfChess.moveList[depth, i];
                }
            }
        }
        return best;//返回极大值
    }
    /// <summary>
    /// Alpha-Beta剪枝算法
    /// </summary>
    /// <param name="depth">当前距离叶子节点的距离</param>
    /// <param name="alpha">搜索的上边界</param>
    /// <param name="beta">搜索的下边界</param>
    /// <returns></returns>
    private int AlphaBeta(int depth, int alpha, int beta)
    {
        int score, count, willKillKing,targetChessID;
        willKillKing = IsGameOver(tempBoard, depth);
        if (willKillKing != 0)
            return willKillKing;
        if (depth <= 0)
        {
            return Evaluate(tempBoard, IsEvenLayer(depth));
        }
            
        count = gameManager.movingOfChess.CreatePossibleMove(tempBoard, depth, IsOddLayer(depth));

        for (int i = 0; i < count; ++i)
        {
            targetChessID=MakeMove(gameManager.movingOfChess.moveList[depth,i]);
            score = -AlphaBeta(depth - 1, -beta, -alpha);
            UnMakeMove(gameManager.movingOfChess.moveList[depth, i], targetChessID);
            if (score > alpha)//根据下层返回的极大值更新[alpha,beta]的下界，同时如果当前为顶层记录该最佳着法
            {
                alpha = score;
                if (depth == searchDepth)
                {
                    bestStep = gameManager.movingOfChess.moveList[depth, i];
                }
            }
            //当前不满足alpha<beta，无需继续更新了，后续搜索无意义，剪枝
            if(alpha>=beta)
                break;
        }
        //返回极大值，也可以考虑用一个单独的变量存储，但是没必要
        //因为每次遍历完成下层后需要更新alpha，向大的方向收缩
        return alpha;
    }
    /// <summary>
    /// Fail-soft alpha-beta搜索算法，和Alpha-Beta算法效率几乎无差别
    /// </summary>
    /// <param name="depth"></param>
    /// <param name="alpha"></param>
    /// <param name="beta"></param>
    /// <returns></returns>
    private int FAlphaBeta(int depth, int alpha, int beta)
    {
        //最佳得分
        int best = -INFINITY;
        //当前调用的得分
        int score;
        //当前局面下一步总共可以走的着法
        int count;
        // 改棋移动目标位置原来的ID值,用来还原，chessList仅保存了位置索引
        int targetChessID;
        int willKillKing = IsGameOver(tempBoard, depth);
        if (willKillKing != 0)
        {
            //棋局结束，将帅有阵亡
            return willKillKing;
        }
        //到达底层，直接返回评估值
        if (depth <= 0)
        {
            return Evaluate(tempBoard, IsEvenLayer(depth));

        }
        //获取所有可能的着法
        count = gameManager.movingOfChess.CreatePossibleMove(tempBoard, depth, IsOddLayer(depth));
        for (int i = 0; i < count; ++i)
        {
            targetChessID = MakeMove(gameManager.movingOfChess.moveList[depth, i]);
            score = -FAlphaBeta(depth - 1,-beta,-alpha);
            UnMakeMove(gameManager.movingOfChess.moveList[depth, i], targetChessID);
            //当前分值大于目前最佳，至少会更新一次，初始为best初始为-INF
            //后续如果不在我们预期的[alpha,beta]区间内，则会最终保留更新中最佳的那一次
            if (score > best)
            {
                best = score;
                if (depth == searchDepth)//在根节点记录最佳着法
                    bestStep = gameManager.movingOfChess.moveList[depth, i];
                if (score >= alpha)
                    alpha = score;
                if(alpha>=beta)//剪枝
                    break;
            }
        }
        return best;//返回最佳值或者边界

    }
    /// <summary>
    /// 渴望搜索，当4层以上的搜索时明显快于Alpha-Beta但是不实用
    /// </summary>
    /// <param name="depth"></param>
    /// <returns></returns>
    private int AspirationSearch(int depth)
    {

        int score = 0;//最终分数
        //减少层数获取一个大致的x作为猜测值
        int x = FAlphaBeta(depth - 1, -INFINITY, INFINITY);
        int y = FAlphaBeta(depth, x - 50, x + 50);
        score = y;
        if (y < x - 50)
            score=FAlphaBeta(depth, -INFINITY, x - 50);
        if (y > x + 50)
            score=FAlphaBeta(depth, x + 50, INFINITY);
        return score;
    }
    /// <summary>
    /// 极小窗口搜索算法（Minimal Window Search/PVS）
    /// 该算法基于假设：第一个分支结点是最佳移动，其后次之，直到另一结点被证明是更优的。
    /// 第一个分支结点采用完整搜索[-INFINITY,INFINITY]，并搜索得到第一个分支的最佳走法v。
    /// 随后的分支采用极小的窗口[v,v+1]继续搜索，从而极大的提高效率。因此该算法依赖于假设中第一个结点最优。后续
    /// 结点的搜索相对于前驱变化不大，否则出现fail high需要以[v+1,k]的范围重新搜索，如果出现fail low说明不如当前最佳
    /// 不必继续搜索。
    /// </summary>
    /// <param name="depth"></param>
    /// <param name="alpha"></param>
    /// <param name="beta"></param>
    /// <returns></returns>
    private int PrincipalVariation(int depth, int alpha, int beta)
    {
        int score,willKillKing,targetChessID,count,best;
        willKillKing = IsGameOver(tempBoard, depth);
        if (willKillKing != 0)
            return willKillKing;
        if (depth <= 0)
            return Evaluate(tempBoard, IsEvenLayer(depth));
        count = gameManager.movingOfChess.CreatePossibleMove(tempBoard, depth, IsOddLayer(depth));
        //可能count为0，数组越界异常?
        targetChessID = MakeMove(gameManager.movingOfChess.moveList[depth, 0]);
        //第一个结点分支采用完整搜索
        best = -PrincipalVariation(depth - 1, -beta, -alpha);
        UnMakeMove(gameManager.movingOfChess.moveList[depth, 0], targetChessID);
        if (depth == searchDepth)
            bestStep = gameManager.movingOfChess.moveList[depth, 0];
        for (int i = 1; i < count; ++i)//对与第二个结点依赖与第一次搜索结果best
        {
            if (best < beta)//不满足则发生Beta剪枝
            {
                if (best > alpha)
                    alpha = best;
                //尝试移动并搜索下层
                targetChessID = MakeMove(gameManager.movingOfChess.moveList[depth, i]);
                score = -PrincipalVariation(depth - 1, -alpha - 1, -alpha);//相当于极大极小的 [alpha,alpha+1]
                if (score > alpha && score < beta)//即存在比alpha更优的解，同时小于beta不会剪枝
                {
                    //fail high，加大窗口，重新搜索
                    best = -PrincipalVariation(depth - 1, -beta, -score);
                    if (depth == searchDepth)
                        bestStep = gameManager.movingOfChess.moveList[depth, i];
                }
                else if (score>best)//窄窗搜索命中，则更新最优解
                {
                    best = score;
                    if (depth == searchDepth)
                        bestStep = gameManager.movingOfChess.moveList[depth, i];
                }
                //其他情况为该子节点下找到的为low fail，则不是最优解，或者high fail但大于beta发生剪枝
                UnMakeMove(gameManager.movingOfChess.moveList[depth,i],targetChessID);
            }
        }
        return best;//返回最佳值
    }
    /// <summary>
    /// 历史启发Alpha-Beta算法
    /// </summary>
    /// <param name="depth"></param>
    /// <param name="alpha"></param>
    /// <param name="beta"></param>
    /// <returns></returns>
    private int HistoryHeuristicAlphaBeta(int depth, int alpha, int beta)
    {
        int targetChessID;
        int willKillKing = IsGameOver(tempBoard, depth);
        if (willKillKing != 0)
            return willKillKing;
        if (depth <= 0)
            return Evaluate(tempBoard, IsEvenLayer(depth));
        int count = gameManager.movingOfChess.CreatePossibleMove(tempBoard, depth, IsOddLayer(depth));
        MergeSort(gameManager.movingOfChess.moveList, count, depth);//归并排序的过程中，查询启发表来实现排序
        int bestStepIndex = -1;
        for (int i = 0; i < count; i++)
        {
            targetChessID = MakeMove(gameManager.movingOfChess.moveList[depth, i]);
            int score = -HistoryHeuristicAlphaBeta(depth - 1, -beta, -alpha);
            UnMakeMove(gameManager.movingOfChess.moveList[depth, i], targetChessID);
            if (score > alpha)
            {
                alpha = score;
                if (depth == searchDepth)
                    bestStep = gameManager.movingOfChess.moveList[depth, i];
                bestStepIndex = i;//记录最佳得分位置
            }
            if (alpha >= beta)//记录剪枝位置
            {
                bestStepIndex = i;
                break;
            }
        }
        if (bestStepIndex != -1)
        {
            AddHistoryScore(gameManager.movingOfChess.moveList[depth, bestStepIndex], depth);
        }
        return alpha;
    }

    private int AlphaBeta_TT(int depth,int alpha,int beta)
    {
        int targetChessID;
        int willKillKing = IsGameOver(tempBoard, depth);
        if (willKillKing != 0)//是否结束
            return willKillKing;
        int side = (searchDepth - depth) % 2;//0为极大，1为极小
        //查看置换表是否有当前结点的有效数据
        var score = LookUpHashTable(alpha, beta, depth, side);
        if (score != 666666)//查表命中
            return score;
        if (depth <= 0)//叶子结点
        {
            score = Evaluate(tempBoard, IsEvenLayer(depth));
            EnterHashTable(ENTRY_TYPE.EXACT, score, depth, side);//添加到置换表
            return score;
        }
        //获取所有可能走法
        int count = gameManager.movingOfChess.CreatePossibleMove(tempBoard, depth, IsOddLayer(depth));
        int evalIsExact = 0;
        //第一次 a,b为全窗口搜索，后续为b=a+1，为空窗口探测
        for (int i = 0; i < count; i++)
        {
            //产生哈希值
            HashMakeMove(gameManager.movingOfChess.moveList[depth, i]);
            targetChessID = MakeMove(gameManager.movingOfChess.moveList[depth, i]);
            score = -AlphaBeta_TT(depth - 1, -beta, -alpha);
            HashUnMakeMove(gameManager.movingOfChess.moveList[depth, i], targetChessID);
            UnMakeMove(gameManager.movingOfChess.moveList[depth, i], targetChessID);
            if (beta <= score)//剪枝
            {
                EnterHashTable(ENTRY_TYPE.LOWER_BOUND,score,depth,side);
                return score;
            }
            if (score>alpha)//beta剪枝
            {
                alpha = score;
                evalIsExact = 1;
                if (depth == searchDepth)
                    bestStep = gameManager.movingOfChess.moveList[depth, i];
            }
        }
        //搜索结果加入置换表
        if (evalIsExact == 1)
            EnterHashTable(ENTRY_TYPE.EXACT, alpha, depth, side);
        else
            EnterHashTable(ENTRY_TYPE.UPPER_BOUND, alpha, depth, side);
        return alpha;
    }

    /// <summary>
    /// 历史启发+置换表+Alpha-Beta算法
    /// </summary>
    /// <param name="depth"></param>
    /// <param name="alpha"></param>
    /// <param name="beta"></param>
    /// <returns></returns>
    private int NegaScout_HH_TT(int depth, int alpha, int beta)
    {
        int targetChessID;
        int a, b;
        int result = IsGameOver(tempBoard, depth);
        if (result != 0)//是否结束
            return result;
        int side = (searchDepth - depth) % 2;//0为极大，1为极小
        //查看置换表是否有当前结点的有效数据
        var score = LookUpHashTable(alpha, beta, depth, side);
        if (score != 666666)//查表命中
            return score;
        if (depth <= 0)//叶子结点
        {
            score = Evaluate(tempBoard, IsEvenLayer(depth));
            EnterHashTable(ENTRY_TYPE.EXACT,score,depth,side);//添加到置换表
            return score;
        }
        //获取所有可能走法
        int count = gameManager.movingOfChess.CreatePossibleMove(tempBoard, depth, IsOddLayer(depth));
        //根据历史得分排序
        MergeSort(gameManager.movingOfChess.moveList,count,depth);
        int bestStepIndex = -1;//记录谁将记录到启发表中
        a = alpha;
        b = beta;
        int evalIsExact = 0;
        //第一次 a,b为全窗口搜索，后续为b=a+1，为空窗口探测
        for (int i = 0; i < count; i++)
        {
            //产生哈希值
            HashMakeMove(gameManager.movingOfChess.moveList[depth,i]);
            targetChessID = MakeMove(gameManager.movingOfChess.moveList[depth, i]);
            score = -NegaScout_HH_TT(depth - 1, -b, -a);
            if (a < score && score < beta && i > 0)//第一个后的节点,fail high
            {
                a = -NegaScout_HH_TT(depth - 1, -beta, -score);
                evalIsExact = 1;//设置为精确值
                if (depth == searchDepth)
                    bestStep = gameManager.movingOfChess.moveList[depth, i];
                bestStepIndex = i;//记录该位置，后续加入启发表
            }
            HashUnMakeMove(gameManager.movingOfChess.moveList[depth,i],targetChessID);
            UnMakeMove(gameManager.movingOfChess.moveList[depth,i],targetChessID);
            if (a < score)//第一次搜索命中
            {
                evalIsExact = 1;
                a = score;
                if (depth == searchDepth)
                    bestStep = gameManager.movingOfChess.moveList[depth, i];
            }
            if (a >= beta)//beta剪枝
            {
                //将下边界存入置换表
                EnterHashTable(ENTRY_TYPE.LOWER_BOUND,a,depth,side);
                AddHistoryScore(gameManager.movingOfChess.moveList[depth,i],depth);
                return a;
            }

            b = a + 1;//第二次循环开始，设置空窗口进行扫描窗口
        }
        //将最佳着法记录历史启发表
        if (bestStepIndex != -1)
            AddHistoryScore(gameManager.movingOfChess.moveList[depth,bestStepIndex],depth);
        //搜索结果加入置换表
        if(evalIsExact==1)
            EnterHashTable(ENTRY_TYPE.EXACT,a,depth,side);
        else
            EnterHashTable(ENTRY_TYPE.UPPER_BOUND,a,depth,side);
        return a;
    }
    #region 历史启发相关
    //历史启发表，每一种着法有相应的历史得分
    private int [,]historyTable=new int[90,90];
    private ChessReseting.ChessStep[] tempList = new ChessReseting.ChessStep[80];//归并排序临时队列
    /// <summary>
    /// 重置历史得分
    /// </summary>
    private void ResetHistoryTable()
    {
        for (int i = 0; i < 90; i++)
        {
            for (int j = 0; j < 90; j++)
            {
                historyTable[i, j] = 0;
            }
        }
    }
    /// <summary>
    /// 将该着法记录到历史启发表中
    /// </summary>
    /// <param name="move"></param>
    /// <param name="depth"></param>
    private void AddHistoryScore(ChessReseting.ChessStep move, int depth)
    {
        int from = move.from.x * 9 + move.from.y;//起始位置
        int to = move.to.x * 9 + move.to.y;//目标位置
        historyTable[from, to] += 2 << depth;
    }
    /// <summary>
    /// 获取相应着法的历史得分
    /// </summary>
    /// <param name="move"></param>
    /// <returns></returns>
    private int GetHistoryScore(ChessReseting.ChessStep move)
    {
        int from = move.from.x * 9 + move.from.y;//起始位置
        int to = move.to.x * 9 + move.to.y;//目标位置
        return historyTable[from, to];
    }
    /// <summary>
    /// 归并排序
    /// </summary>
    /// <param name="move"></param>
    /// <param name="count"></param>
    /// <param name="depth"></param>
    private void MergeSort(ChessReseting.ChessStep[,] moveList, int count, int depth)
    {
        Sort(moveList, 0, count-1, depth);
    }
    private void Sort(ChessReseting.ChessStep[,] moveList, int left, int right, int depth)
    {
        int mid;
        int i, j,t;
        if (left < right)
        {
            mid = (left + right) >> 1;
            Sort(moveList,left,mid,depth);
            Sort(moveList,mid+1,right,depth);
            i = left;
            j = mid + 1;
            t = 0;
            while (i<=mid&&j<=right)
            {
                if (GetHistoryScore(moveList[depth, i]) >= GetHistoryScore(moveList[depth, j]))
                {
                    tempList[t++] = moveList[depth, i++];
                }
                else
                {
                    tempList[t++] = moveList[depth, j++];
                }
            }

            while (i<=mid)
            {
                tempList[t++] = moveList[depth, i++];
            }

            while (j <= right)
            {
                tempList[t++] = moveList[depth, j++];
            }

            t = 0;
            while (left<=right)
            {
                moveList[depth, left++] = tempList[t++];
            }
        }

    }
    #endregion
    #region 置换表相关

    enum ENTRY_TYPE
    {
        EXACT,
        LOWER_BOUND,
        UPPER_BOUND
    };
    class HashItem
    {
        public long checkSum;//64位校验码
        public ENTRY_TYPE entry_type;//类型
        public int depth;//层次
        public int eval;//结点价值
    }
    //0存放极大值，1存放极小值
    private HashItem[,] hashItemList;
    private int [,,] hashKeyList32;
    private long[,,] hashKeyList64;
    private int hashKey32;//32位哈希值
    private long hashKey64;//64位哈希值
    private static Random random = new Random(unchecked((int)DateTime.Now.Ticks));
    int Rand32()
    {
        //防止溢出
        return random.Next(1 << 30);
    }

    long Rand64()
    {
        return  ((long)Rand32() << 32) + Rand32();
    }
    /// <summary>
    /// 生成每种棋子的棋盘位置对于的哈希值,用于后续计算棋盘的哈希值
    /// </summary>
    private void InitializeHashKey()
    {
        int i, j, k;
        hashKeyList32 = new int[15, 10, 9];
        hashKeyList64 = new long[15, 10, 9];
        for (i = 0; i < 15; i++)
        {
            for (j = 0; j < 10; j++)
            {
                for (k = 0; k < 9; k++)
                {
                    hashKeyList32[i, j, k] = Rand32();
                    hashKeyList64[i, j, k] = Rand64();
                    if(hashKeyList32[i,j,k]<0||hashKeyList64[i,j,k]<0)
                        Debug.LogError("哈希值初始化错误");
                }
            }
        }
        
        hashItemList = new HashItem[2,1024 * 1024];
        for (i = 0; i < 2; i++)
        {
            for (j = 0; j < 1024*1024; j++)
            {
                hashItemList[i, j] = new HashItem();
            }
        }
        Debug.Log("棋子哈希值初始化完成");
    }
    /// <summary>
    /// 根据传入的棋盘计算出32位的哈希值
    /// </summary>
    /// <param name="position"></param>
    private void CalculateInitHashKey(int[,] position)
    {
        int chessID;
        hashKey32 = 0;
        hashKey64 = 0;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                chessID = position[i, j];
                if (chessID != 0)//存在棋子
                {
                    hashKey32 ^= hashKeyList32[chessID,i, j];
                    hashKey64 ^= hashKeyList64[chessID, i, j];
                }
            }
        }
    }
    /// <summary>
    /// 根据传入的走法，修改相应的哈希值
    /// </summary>
    /// <param name="move"></param>
    private void HashMakeMove(ChessReseting.ChessStep move)
    {
        int toID, fromID;
        fromID = tempBoard[move.from.x, move.from.y];
        toID = tempBoard[move.to.x, move.to.y];
        //将要移动的棋子删除
        hashKey32 ^= hashKeyList32[fromID, move.from.x, move.from.y];
        hashKey64 ^= hashKeyList64[fromID, move.from.x, move.from.y];
        //目标位置有棋子删除
        if (toID != 0)
        {
            hashKey32 ^= hashKeyList32[toID, move.to.x, move.to.y];
            hashKey64 ^= hashKeyList64[toID, move.to.x, move.to.y];
        }
        //移动后加上目标为自的哈希值
        hashKey32 ^= hashKeyList32[fromID, move.to.x, move.to.y];
        hashKey64 ^= hashKeyList64[fromID, move.to.x, move.to.y];

    }
    /// <summary>
    /// 恢复HashMake修改的哈希值
    /// </summary>
    /// <param name="move"></param>
    /// <param name="chessID"></param>
    private void HashUnMakeMove(ChessReseting.ChessStep move,int chessID)
    {
        int toID = tempBoard[move.to.x, move.to.y];
        hashKey32 ^= hashKeyList32[toID, move.from.x, move.from.y];//加上原来位置的数
        hashKey64 ^= hashKeyList64[toID, move.from.x, move.from.y];
        hashKey32 ^= hashKeyList32[toID, move.to.x, move.to.y];//删除当前的位置
        hashKey64 ^= hashKeyList64[toID, move.to.x, move.to.y];
        if (chessID!=0)//被吃掉棋子的哈希值加上
        {
            hashKey32 ^= hashKeyList32[chessID, move.to.x, move.to.y];
            hashKey64 ^= hashKeyList64[chessID, move.to.x, move.to.y];
        }

    }
    /// <summary>
    /// 查找哈希表
    /// </summary>
    /// <param name="alpha">搜索下边界</param>
    /// <param name="beta">搜索上边界</param>
    /// <param name="depth">当前搜索层次</param>
    /// <param name="tableNo">奇数还是偶数层</param>
    /// <returns></returns>
    private int LookUpHashTable(int alpha, int beta, int depth, int tableNo)
    {
        int x = hashKey32 & 0xfffff;
        HashItem t = hashItemList[tableNo, x];
        if(t==null)
            Debug.Log("null error");
        if (t.depth >= depth && t.checkSum == hashKey64)
        {
            switch (t.entry_type)
            {
                case ENTRY_TYPE.EXACT://确切值
                    return t.eval;
                case ENTRY_TYPE.LOWER_BOUND://下边界
                    if (t.eval >= beta)
                        return t.eval;
                    else
                        break;
                case ENTRY_TYPE.UPPER_BOUND://上边界
                    if (t.eval <= alpha)
                        return t.eval;
                    else
                        break;
            }
        }
        return 666666;//无效标志
    }
    /// <summary>
    /// 在置换表中加入新表项
    /// </summary>
    /// <param name="entryType">表项类型</param>
    /// <param name="eval">价值</param>
    /// <param name="depth">当前搜索层次</param>
    /// <param name="tableNo">奇数还是偶数层</param>
    private void EnterHashTable(ENTRY_TYPE entryType, int eval, int depth, int tableNo)
    {
        int x=hashKey32 & 0xfffff;//获取地址
        HashItem t = hashItemList[tableNo, x];
        t.checkSum = hashKey64;//设置验证码
        t.entry_type = entryType;//表项类型
        t.eval = eval;//价值
        t.depth = depth;//深度
    }
    #endregion
    /// <summary>
    /// 评估函数
    /// </summary>
    /// <param name="position"></param>
    /// <param name="isRedTurn">当前是否为红方轮次</param>
    /// <returns></returns>
    private int Evaluate(int [,]position,bool isRedTurn)
    {
        evaluateCallTime++;
        int currentPosChessID;//当前棋子ID
        int targetPosChessID;//目标位置ID
        chessValue = new int[10, 9];//总价值
        attackPos = new int[10, 9];//威胁度
        guardPos = new int[10, 9];//保护度
        flexPos = new int[10, 9];//灵活度
        //第一次扫描计算每个棋子提供相关位置的灵活值、威胁值和保护值。
        //随后为每个有棋子的位置设置最终分值
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (position[i, j] != 0)//存在棋子
                {
                    currentPosChessID = position[i, j];
                    //获取所有相关位置的索引
                    GetRelatePos(position, i, j);
                    //遍历所有相关位置，基于分值
                    for (int k = 0; k < posCount; k++)
                    {
                        //所在位置的ID
                        targetPosChessID = position[relatedPos[k].x,relatedPos[k].y];
                        if (targetPosChessID == 0)//空格,代表我们该位置灵活值增加
                        {
                            flexPos[i, j]++;
                        }
                        //同方棋子，相关位置保护值增加
                        else if(gameManager.rules.IsSameSide(currentPosChessID,targetPosChessID))
                        {
                            guardPos[relatedPos[k].x,relatedPos[k].y]++;
                        }
                        //敌方棋子，该位置灵活值增加，相关位置威胁值增加
                        else
                        {
                            attackPos[relatedPos[k].x, relatedPos[k].y]++;
                            flexPos[i,j]++;
                            switch (targetPosChessID)
                            {
                                // 击杀敌方的将帅，返回最大值
                                case 8: //黑方轮次能够杀死帅
                                    if (!isRedTurn)
                                    {
                                        return 18888;
                                    }
                                    break;
                                case 1: //红方轮次能够杀死将
                                    if (isRedTurn)
                                    {
                                        return 18888;
                                    }
                                    break;
                                //非将帅的其他棋子，根据威胁的棋子加上威胁分值
                                default:
                                    attackPos[relatedPos[k].x, relatedPos[k].y] +=
                                        ((baseValue[targetPosChessID] - baseValue[currentPosChessID]) / 10 + 30) / 10;
                                    break;
                            }
                        }
                    }
                    //最终设置chessValue
                    chessValue[i, j]++;//有棋子存在其价值不为0
                    //每个棋子的灵活行价值加入棋子总价值
                    chessValue[i, j] += flexValue[currentPosChessID] * flexPos[i, j];
                    //若是小兵，需要加上小兵位置的进攻附加值
                    chessValue[i, j] += GetBingExtraValue(i, j, position);

                }
            }
        }
        
        //第二次扫描，根据前面计算的威胁值和保护值等，即棋子之间的相互关系更新chessValue
        int delta;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (position[i, j] != 0)
                {
                    currentPosChessID = position[i, j];
                    //将棋子的基础值的16分之一作为威胁/保护增量
                    delta = baseValue[currentPosChessID] / 16;
                    //棋子的基础价值加入总价值
                    chessValue[i, j] += baseValue[currentPosChessID];
                    //红色棋子
                    if (gameManager.rules.IsRed(currentPosChessID))
                    {
                        //该位置被威胁
                        if (attackPos[i, j] != 0)
                        {
                            //红方轮次
                            if (isRedTurn)
                            {
                                //红方轮次当前帅被威胁，有威胁但是自己的轮次，减去一定分值
                                if (currentPosChessID == 8)
                                {
                                    chessValue[i, j] -= 20;
                                }
                                else
                                {
                                    //基础值减去2倍威胁增量
                                    chessValue[i, j] -= 2 * delta;
                                    //保护值不为0，基础值增加保护增量
                                    if (guardPos[i, j] != 0)
                                    {
                                        chessValue[i, j] += delta;
                                    }
                                }
                            }
                            //黑方轮次
                            else
                            {
                                //当前威胁帅，同时黑方轮次，意味着AI可以胜利
                                if (currentPosChessID == 8) 
                                {
                                    return 18888;
                                }
                                //对方的轮次，则-10倍威胁增量表示威胁程度大
                                chessValue[i,j] -= 10 * delta;
                                //被保护，增加分值
                                if (guardPos[i, j] != 0)
                                {
                                    chessValue[i, j] += delta * 9;
                                }
                            }
                            
                        }
                        //没有威胁
                        else
                        {
                            //受到保护，加点分
                            if (guardPos[i, j] != 0)
                            {
                                chessValue[i, j] += 5;
                            }
                        }
                    }
                    //黑棋
                    else
                    {
                        //被威胁
                        if (attackPos[i, j] != 0)
                        {
                            //黑方轮次
                            if (!isRedTurn)
                            {
                                //黑将被威胁，当前黑方轮次，不会将死，减去少量分值
                                if (currentPosChessID == 1)//黑将
                                {
                                    chessValue[i,j] -= 20;
                                }
                                else
                                {
                                    //减去2倍威胁增量
                                    chessValue[i, j] -= attackPos[i, j] * 2;
                                    //加上保护增量
                                    if (guardPos[i, j] != 0)
                                    {
                                        chessValue[i, j] += delta;
                                    }
                                }
                            }
                            //红色轮次
                            else
                            {
                                // 红方轮次，同时将已经被威胁，红方可以胜利
                                if (currentPosChessID == 1)
                                {
                                    return 18888;
                                }
                                //对方的轮次，则-10倍威胁增量表示威胁程度大
                                chessValue[i, j] -= attackPos[i, j] * 10;
                                //被保护，增加分值
                                if (guardPos[i, j] != 0)
                                {
                                    chessValue[i, j] += delta * 9;
                                }
                            }
                        }
                        else
                        {
                            //被保护，增加少量分值
                            if (guardPos[i, j] != 0)
                            {
                                chessValue[i, j] += 5;
                            }
                        }
                    }
                }
            }
        }

        //第三次扫描，统计整个棋盘红黑方总的的chessValue
        int redScore = 0;
        int blackScore = 0;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                currentPosChessID = position[i, j];
                if (position[i, j] != 0)
                {
                    //红色
                    if (gameManager.rules.IsRed(currentPosChessID))
                    {
                        redScore += chessValue[i, j];
                    }
                    //黑色
                    else if (gameManager.rules.IsBlack(currentPosChessID))
                    {
                        blackScore += chessValue[i, j];
                    }
                }
            }
        }
        if (isRedTurn)//红方
        {
            return redScore - blackScore;
        }
        else//黑方
        {
            return blackScore - redScore;
        }
    }

    /// <summary>
    /// 根据move更新tempBoard
    /// </summary>
    /// <param name="move"></param>
    /// <returns>移动前目标位置的ID</returns>
    private int MakeMove(ChessReseting.ChessStep move)
    {
        int chessID = tempBoard[move.to.x, move.to.y];
        //移动
        tempBoard[move.to.x, move.to.y] = tempBoard[move.from.x,move.from.y];
        tempBoard[move.from.x,move.from.y] = 0;
        return chessID;//后续需要撤销该步棋，需要知道目标位置原来存放什么
    }
    /// <summary>
    /// 撤销move
    /// </summary>
    /// <param name="move"></param>
    /// <param name="chessID"></param>
    private void UnMakeMove(ChessReseting.ChessStep move,int chessID)
    {
        tempBoard[move.from.x, move.from.y] = tempBoard[move.to.x, move.to.y];
        tempBoard[move.to.x, move.to.y] = chessID;
    }

    /// <summary>
    /// 判断游戏是否结束，返回相应的分数
    /// 返回的分数是相对于当前轮次的玩家来说
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="depth"></param>
    /// <returns></returns>
    private int IsGameOver(int[,] position, int depth)
    {
        bool redAlive=false, blackAlive=false;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 3; j < 6; j++)
            {
                if (position[i, j] == 1)
                {
                    blackAlive = true;
                }
            }
        }
        for (int i = 7; i < 10; i++)
        {
            for (int j = 3; j < 6; j++)
            {
                if (position[i, j] == 8)
                {
                    redAlive = true;
                }
            }
        }
        // 获取当前的轮次，true为AI，false为玩家，需要根据当前局势是相对谁而言，返回对应的正负得分
        bool isOdd = IsOddLayer(depth);
        if (!redAlive)//帅不存在
        {
            if (isOdd)//奇数AI层，帅击杀，对AI最优
            {
                //Debug.Log("黑方击杀帅");
                return 19990+depth;
            }
            else//偶数玩家层，帅击杀，对玩家最劣
            {
                //Debug.Log("红方帅被击杀");
                return -19990-depth;
            }

        }
        if (!blackAlive)//将不存在
        {
            if (isOdd) //奇数AI层，将击杀，对AI最劣
            {
                //Debug.Log("黑方将被击杀");
                return -19990-depth;
            }
            else//偶数玩家层，将击杀，对玩家最优
            {
                //Debug.Log("红方击杀将");
                return 19990+depth;
            }
        }
        //均存在返回0
        return 0;
    }
    /// <summary>
    /// 判断当前层是否是奇数层，即AI层
    /// true为AI层，false为玩家层
    /// </summary>
    /// <param name="depth"></param>
    /// <returns></returns>
    private bool IsOddLayer(int depth)
    {
        return (searchDepth - depth + 1) % 2==1;
    }
    /// <summary>
    /// 判断当前是否为偶数层，即玩家层
    /// true为玩家层，false为AI层
    /// </summary>
    /// <param name="depth"></param>
    /// <returns></returns>
    private bool IsEvenLayer(int depth)
    {
        return !IsOddLayer(depth);
    }
    /// <summary>
    /// 获取指定位置相关的位置
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    /// <returns></returns>
    private int GetRelatePos(int[,] position, int fromX, int fromY)
    {
        posCount = 0;//首先清空
        int chessID = position[fromX, fromY];
        bool flag = false;
        int x, y;
        switch (chessID)
        {
            case 1://黑将
                for (x = 0; x < 3; x++)
                {
                    for (y = 3; y < 6; y++)
                    {
                        //判断位置是否合法
                        if(CanReach(position,fromX,fromY,x,y))
                            AddPos(x,y);
                    }
                }
                break;
            case 8://红帅
                for (x = 7; x < 10; x++)
                {
                    for (y = 3; y < 6; y++)
                    {
                        if (CanReach(position, fromX, fromY, x, y))
                            AddPos(x, y);
                    }
                }
                break;
            case 2://黑车
            case 9://红车
                //上
                x = fromX - 1;
                y = fromY;
                while (x >= 0)
                {
                    if (position[x, y] == 0)
                    {
                        AddPos(x,y);
                    }
                    else
                    {
                        AddPos(x, y);
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
                        AddPos(x, y);
                    }
                    else
                    {
                        AddPos(x, y);
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
                        AddPos(x, y);
                    }
                    else
                    {
                        AddPos(x, y);
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
                        AddPos(x, y);
                    }
                    else
                    {
                        AddPos(x, y);
                        break;
                    }
                    y++;
                }
                break;
            case 3://黑马
            case 10:
                x = fromX - 2;
                y = fromY - 1;
                if (x >= 0 && y >= 0 && CanReach(position, fromX, fromY, x, y))
                {
                    AddPos(x,y);
                }
                //左下
                x = fromX + 2;
                y = fromY - 1;
                if (x < 10 && y >= 0 && CanReach(position, fromX, fromY, x, y))
                {
                    AddPos(x,y);
                }
                //右上
                x = fromX - 2;
                y = fromY + 1;
                if (x >= 0 && y < 9 && CanReach(position, fromX, fromY, x, y))
                {
                    AddPos(x,y);
                }
                //右下
                x = fromX + 2;
                y = fromY + 1;
                if (x < 10 && y < 9 && CanReach(position, fromX, fromY, x, y))
                {
                    AddPos(x,y);
                }
                //横日
                //左上
                x = fromX - 1;
                y = fromY - 2;
                if (x >= 0 && y >= 0 && CanReach(position, fromX, fromY, x, y))
                {
                    AddPos(x,y);
                }
                //左下
                x = fromX + 1;
                y = fromY - 2;
                if (x < 10 && y >= 0 && CanReach(position, fromX, fromY, x, y))
                {
                    AddPos(x, y);
                }
                //右上
                x = fromX - 1;
                y = fromY + 2;
                if (x >= 0 && y < 9 && CanReach(position, fromX, fromY, x, y))
                {
                    AddPos(x, y);
                }
                //右下
                x = fromX + 1;
                y = fromY + 2;
                if (x < 10 && y < 9 && CanReach(position, fromX, fromY, x, y))
                {
                    AddPos(x,y);
                }
                break;
            case 4://黑炮
            case 11:
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
                            AddPos(x, y);
                        }
                    }
                    else
                    {
                        if (!flag)//经过棋子时，开启翻山条件
                        {
                            flag = true;
                        }
                        //已经翻山了，添加后结束
                        else
                        {
                            AddPos(x,y);
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
                            AddPos(x, y);
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
                            AddPos(x, y);
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
                            AddPos(x, y);
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
                            AddPos(x, y);
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
                            AddPos(x, y);
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
                            AddPos(x, y);
                            break;
                        }
                    }
                    y++;
                }
                break;
            case 5://黑士
                for (x = 0; x < 3; x++)
                {
                    for (y = 3; y < 6; y++)
                    {
                        if (CanReach(position, fromX, fromY, x, y))
                            AddPos(x,y);
                    }
                }
                break;
            case 12://红仕
                for (x = 7; x < 10; x++)
                {
                    for (y = 3; y < 6; y++)
                    {
                        if (CanReach(position, fromX, fromY, x, y))
                            AddPos(x,y);
                    }
                }
                break;
            case 6://黑象
            case 13:
                //左上
                x = fromX - 2;
                y = fromY - 2;
                if (x >= 0 && y >= 0 && CanReach(position, fromX, fromY, x, y))
                {
                    AddPos(x,y);
                }
                //左下
                x = fromX + 2;
                y = fromY - 2;
                if (x < 10 && y >= 0 && CanReach(position, fromX, fromY, x, y))
                {
                    AddPos(x, y);
                }
                //右上
                x = fromX - 2;
                y = fromY + 2;
                if (x >= 0 && y < 9 && CanReach(position, fromX, fromY, x, y))
                {
                    AddPos(x, y);
                }
                //右下
                x = fromX + 2;
                y = fromY + 2;
                if (x < 10 && y < 9 && CanReach(position, fromX, fromY, x, y))
                {
                    AddPos(x, y);
                }
                break;
            case 7://黑卒
                x = fromX + 1;
                y = fromY;
                if (x < 10)
                {
                    AddPos(x,y);
                }

                if (fromX > 4)
                {
                    x = fromX;
                    //左边
                    y = fromY - 1;
                    if (y >= 0)
                    {
                        AddPos(x, y);
                    }
                    //右边
                    y = fromY + 1;
                    if (y < 9)
                    {
                        AddPos(x, y);
                    }
                }
                break;
            case 14:
                x = fromX - 1;//向上移动
                y = fromY;
                if (x >= 0)
                {
                    AddPos(x,y);
                }

                if (fromX < 5)
                {
                    //左边
                    x = fromX;
                    y = fromY - 1;
                    if (y >= 0)
                    {
                        AddPos(x,y);
                    }
                    //右边
                    y = fromY + 1;
                    if (y < 9)
                    {
                        AddPos(x,y);
                    }
                }
                break;
            default:
                break;
        }
        //Debug.Log(position[fromX,fromY]+"相关位置数量为"+posCount);
        return posCount;
    }
    /// <summary>
    /// 添加到相关位置到数组中
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private void AddPos(int x, int y)
    {
        relatedPos[posCount].x = x;
        relatedPos[posCount].y = y;
        posCount++;
    }
    /// <summary>
    /// 判断位置是否在棋子的移动范围内，无需讨论是否同方棋子
    /// </summary>
    /// <param name="position"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    /// <param name="toX"></param>
    /// <param name="toY"></param>
    /// <returns></returns>
    private bool CanReach(int[,] position,int fromX,int fromY,int toX,int toY)
    {
        return gameManager.rules.IsValid(position[fromX, fromY], position, fromX, fromY, toX, toY);
    }

    /// <summary>
    /// 添加小兵位置附加值
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    private int GetBingExtraValue(int x, int y, int[,] position)
    {
        //红兵
        if (position[x, y] == 14)
        {
            return r_bingValue[x, y];
        }
        //黑卒
        else if (position[x, y] == 7)
        {
            return b_bingValue[x, y];
        }
        return 0;
    }
    


}
