using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

// <summary>
// 存储游戏数据，游戏引用，游戏资源，模式切换与控制

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int chessPeople; //当前对战人数，PVE 1 PVP 2 联网 3
    public int currentLevel; //当前难度 1.简单 2.一般 3.困难

    /// <summary>
    /// 数据
    /// </summary>
    public int[,] chessBoard;//当前棋盘的状况
    public GameObject[,] boardGrid;//棋盘上的所有格子
    private const float gridWidth = 68;
    private const float gridHeight = 69;
    /// <summary>
    // 开关
    /// </summary>
    public bool redChessMove;//下棋轮次,红true,黑false
    public bool gameOver;//游戏结束,不能走棋
    public bool hasLoad;//游戏内物体是否已经加载
    /// <summary>
    /// 资源，用来生成游戏对象
    /// </summary>
    public GameObject gridGo;//格子
    public Sprite[] sprites;//所有棋子的sprite
    public GameObject chessGo;//棋子
    public GameObject canMovePosUIGo;//可移动位置UI显示
    public GameObject canEatPosUIGo;//可吃棋子的UI显示
    /// <summary>
    /// 引用，GameManager存放其他脚本对象，其他脚本通过GameManager可以使用接口实现功能
    /// </summary>
    [HideInInspector]
    public GameObject boardGo;//棋盘,代码控制使用,隐藏Inspector
    public GameObject[] boardGos;//两个棋盘 0.单机模式 1.联网模式
    [HideInInspector]
    public ChessOrGrid lastChessOrGrid;//上一次点击到的对象(格子或者棋子)
    public Rules rules;//游戏移动判断规则类
    public MovingOfChess movingOfChess;//棋子移动类
    public Checkmate checkmate;//将军检测类
    public ChessReseting chessResting;//悔棋类
    public SearchEngine searchEngine;//搜索引擎
    public GameObject eatChessPool;//被吃掉棋子存放的棋子池
    public GameObject clickChessUIGo;//选中棋子的UI显示
    public GameObject lastPosUIGo;//棋子移动前的位置UI显示
    private Stack<GameObject> canMoveUIStack;//移动位置UI的对象栈
    private Stack<GameObject> currentCanMoveUIStack;
    private Stack<GameObject> canEatUIStack;//可吃位置UI的对象栈
    private Stack<GameObject> currentCanEatUIStack;


    /// <summary>
    /// 联网相关
    /// </summary>
    //客户端
    public GameClient gameClient;
    //服务器
    public GameServer gameServer;
    //当前是否为服务器端程序
    public bool isServer;
    //数据包，格式：
    //第0位{0继续1准备2连接完成3对方放弃}
    //第1位发送方颜色{0黑色1红色}
    //第2~5位，代表起始和目标位置索引。
    public int[] gameCodeReceive;
    //准备标志
    public bool redHasReady;
    public bool blackHasReady;
    //收到新消息的开关
    public bool isReceived;
    //当前棋盘是否加载
    private bool netModeHasLoad;
    private void Awake()
    {
        Instance = this;
        gameOver = true;
        hasLoad=netModeHasLoad=false;
        gameCodeReceive = new int[6];
    }

    void Update()
    {
        //若有新数据到达，解析数据并更新棋盘
        if (isReceived)
        {
            isReceived = false;
            ParseGameCode();
        }
    }
    /// <summary>
    /// 重置游戏,第一次会创建游戏对象
    /// </summary>
    public void ResetGame()
    {
        gameOver = false;//游戏状态是否结束
        redChessMove = true;//默认开始为红色轮次
        UIManager.Instance.ShowTip(redChessMove?"红方走":"黑方走");
        if (HasLoad())
            return;
        Debug.Log("开始初始化");
        InitChessBoard();//初始化棋盘数据
        SelectBoard();//根据模式选择棋盘
        InitGrid();//初始化格子对象
        InitChess();//初始化棋子对象
        //规则类对象
        rules = new Rules();
        //移动类对象
        movingOfChess = new MovingOfChess(this);
        //将军检测对象
        checkmate = new Checkmate();
        //悔棋类对象
        chessResting = new ChessReseting();
        chessResting.resetCount = 0;
        chessResting.chessSteps = new ChessReseting.ChessStep[400];//最大400步
        //可移动位置UI栈，预先实例化20个对象，最多如车17个可移动位置
        canMoveUIStack = new Stack<GameObject>();
        for (int i = 0; i < 20; i++)
        {
            canMoveUIStack.Push(Instantiate(canMovePosUIGo));
        }
        //可吃位置UI栈，最多如马有8个位置
        canEatUIStack= new Stack<GameObject>();
        for (int i = 0; i < 10; i++)
        {
            canEatUIStack.Push(Instantiate(canEatPosUIGo));
        }
        currentCanMoveUIStack = new Stack<GameObject>();
        currentCanEatUIStack = new Stack<GameObject>();
        //搜索引擎
        searchEngine = new SearchEngine();
        if (chessPeople == 3)
        {
            netModeHasLoad = true;
        }
        if (chessPeople == 1 || chessPeople == 2)
        {
            hasLoad = true;
        }

    }
    private void SelectBoard()
    {
        //根据人数选取对应棋盘UI对象
        if (chessPeople == 1 || chessPeople == 2)//本地模式
        {
            boardGo = boardGos[0];
        }
        else//联网模式
        {
            boardGo = boardGos[1];
        }
    }
    private bool HasLoad()
    {
        if (chessPeople == 3 && netModeHasLoad)//联网模式，判断是否加载过棋盘
        {
            return true;
        }
        //本地模式，当前游戏物体已经加载
        if ((chessPeople == 1 || chessPeople == 2) && hasLoad)
        {
            return true;
        }
        return false;
    }
    private void InitChessBoard()
    {
        //初始化棋盘
        chessBoard = new int[10, 9]
        {
            {2, 3, 6, 5, 1, 5, 6, 3, 2},
            {0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 4, 0, 0, 0, 0, 0, 4, 0},
            {7, 0, 7, 0, 7, 0, 7, 0, 7},
            {0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0},
            {14, 0, 14, 0, 14, 0, 14, 0, 14},
            {0, 11, 0, 0, 0, 0, 0, 11, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0},
            {9, 10, 13, 12, 8, 12, 13, 10, 9}
        };
    }
    /// <summary>
    /// 实例化格子，格子获取挂载的脚本存放位置索引，锚点（左上角）开始，随后累加
    /// </summary>
    private void InitGrid()
    {
        float posX = 0, posY = 0;
        boardGrid = new GameObject[10, 9];
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                GameObject itemGo = Instantiate(gridGo);//克隆一份
                itemGo.transform.SetParent(boardGo.transform);//设置父对象为棋盘对象
                itemGo.name = "Item[" + i.ToString() + "," + j.ToString()+"]";//修改格子名称
                //相对Parent的距离
                itemGo.transform.localPosition = new Vector3(posX, posY, 0);
                posX += gridWidth;//进入下一列
                itemGo.GetComponent<ChessOrGrid>().xIndex = i;//存储格子索引
                itemGo.GetComponent<ChessOrGrid>().yIndex = j;
                boardGrid[i, j] = itemGo;//存储到格子数组
            }
            posX = 0;
            posY -= gridHeight;//向下Y减少，到下一排
        }
    }
    /// <summary>
    /// 实例化棋子
    /// </summary>
    private void InitChess()
    {
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                GameObject item = boardGrid[i, j];//获取格子，用来放置棋子
                switch (chessBoard[i,j])//根据棋子ID生成棋子
                {
                    case 1:
                        CreateChess(item,"b_jiang",sprites[0],false);
                        break;
                    case 2:
                        CreateChess(item, "b_ju", sprites[1], false);
                        break;
                    case 3:
                        CreateChess(item, "b_ma", sprites[2], false);
                        break;
                    case 4:
                        CreateChess(item, "b_pao", sprites[3], false);
                        break;
                    case 5:
                        CreateChess(item, "b_shi", sprites[4], false);
                        break;
                    case 6:
                        CreateChess(item, "b_xiang", sprites[5], false);
                        break;
                    case 7:
                        CreateChess(item, "b_bing", sprites[6], false);
                        break;
                    case 8:
                        CreateChess(item, "r_shuai", sprites[7], true);
                        break;
                    case 9:
                        CreateChess(item, "r_ju", sprites[8], true);
                        break;
                    case 10:
                        CreateChess(item, "r_ma", sprites[9], true);
                        break;
                    case 11:
                        CreateChess(item, "r_pao", sprites[10], true);
                        break;
                    case 12:
                        CreateChess(item, "r_shi", sprites[11], true);
                        break;
                    case 13:
                        CreateChess(item, "r_xiang", sprites[12], true);
                        break;
                    case 14:
                        CreateChess(item, "r_bing", sprites[13], true);
                        break;
                    default:break;
                }
            }
        }
    }
    /// <summary>
    /// 生成棋子游戏物体，棋子挂载和格子相同脚本，但不存放位置信息
    /// 从父对象格子获取即可
    /// </summary>
    /// <param name="itemGo">作为父对象的格子(存放位置)</param>
    /// <param name="name">棋子名称</param>
    /// <param name="chessIcon">棋子标志样式</param>
    /// <param name="ifRed">是否为红色棋子</param>
    private void CreateChess(GameObject gridItem,string name,Sprite chessIcon,bool ifRed)
    {
        GameObject item = Instantiate(chessGo);//生成一个棋子
        item.transform.SetParent(gridItem.transform);//设置格子为父对象
        item.name = name;//设置棋子对象的名字
        item.GetComponent<Image>().sprite = chessIcon;
        item.transform.localPosition = Vector3.zero;
        item.transform.localScale = Vector3.one;
        item.GetComponent<ChessOrGrid>().isRed = ifRed;
    }
    /// <summary>
    /// 将被吃棋子UI移动到屏幕外
    /// </summary>
    /// <param name="itemGo">被吃掉棋子的游戏物体</param>
    public void BeEat(GameObject itemGo)
    {
        itemGo.transform.SetParent(eatChessPool.transform);
        itemGo.transform.localPosition = Vector3.zero;
    }
    /// <summary>
    /// 单机重玩方法，隐藏UI，恢复到加载后的初始状态（不消灭游戏对象）
    /// </summary>
    public void Replay()
    {
        HideLastPositionUI();
        HideClickUI();
        ClearCurrentCanEatUIStack();
        ClearCurrentCanMoveUIStack();
        lastChessOrGrid = null;
        AudioSourceManager.Instance.MuteCheckMate();
        while (chessResting!=null && chessResting.resetCount > 0)
        {
            chessResting.ResetChess();
        }

        if (chessPeople == 3)
        {
            UIManager.Instance.CanClickButton(true);
            UIManager.Instance.netModeText.text = "匹配";
        }
        AudioSourceManager.Instance.UnMuteCheckMate();
        AudioSourceManager.Instance.PlaySound(5);
    }

    // 后期考虑放到UIManager中，简化GameManager
    #region 关于游戏进行中UI显示隐藏的方法
    /// <summary>
    /// 显示隐藏点击选中棋子的UI
    /// </summary>
    /// <param name="targetTransform">点击棋子的坐标</param>
    public void ShowClickUI(Transform targetTransform)
    {
        clickChessUIGo.transform.SetParent(targetTransform);
        clickChessUIGo.transform.localPosition=Vector3.zero;
    }

    public void HideClickUI()
    {
        clickChessUIGo.transform.SetParent(eatChessPool.transform);
        clickChessUIGo.transform.localPosition=Vector3.zero;
    }
    /// <summary>
    /// 显示棋子移动前的位置
    /// </summary>
    /// <param name="showPosition">移动前棋子的坐标</param>
    public void ShowLastPositionUI(Vector3 showPosition)
    {
        lastPosUIGo.transform.position = showPosition;
    }
    /// <summary>
    /// 隐藏棋子移动前位置UI
    /// </summary>
    public void HideLastPositionUI()
    {
        lastPosUIGo.transform.SetParent(eatChessPool.transform);
        lastPosUIGo.transform.localPosition=Vector3.zero;
    }
    /// <summary>
    /// 当前选中棋子可以移动到的位置UI显示与隐藏
    /// </summary>
    /// <returns>可移动位置UI</returns>
    public GameObject PopCanMoveUI()
    {
        GameObject itemGo = canMoveUIStack.Pop();
        currentCanMoveUIStack.Push(itemGo);
        itemGo.SetActive(true);
        return itemGo;
    }

    public void PushCanMoveUI(GameObject itemGo)
    {
        canMoveUIStack.Push(itemGo);
        itemGo.transform.SetParent(eatChessPool.transform);
        itemGo.SetActive(false);
    }
    public void ClearCurrentCanMoveUIStack()
    {
        if(currentCanMoveUIStack==null)
            return;
        while (currentCanMoveUIStack.Count > 0)
            PushCanMoveUI(currentCanMoveUIStack.Pop());
    }
    /// <summary>
    /// 当前选中棋子可以吃子位置的UI显示与隐藏
    /// </summary>
    /// <returns></returns>
    public GameObject PopCanEatUI()
    {
        GameObject itemGo = canEatUIStack.Pop();
        currentCanEatUIStack.Push(itemGo);
        itemGo.SetActive(true);
        return itemGo;
    }

    public void PushCanEatUI(GameObject itemGo)
    {
        canEatUIStack.Push(itemGo);
        itemGo.transform.SetParent(eatChessPool.transform);
        itemGo.SetActive(false);
    }
    public void ClearCurrentCanEatUIStack()
    {
        if(currentCanEatUIStack==null)
            return;
        while (currentCanEatUIStack.Count > 0)
            PushCanEatUI(currentCanEatUIStack.Pop());
    }

    #endregion

    #region 联网相关方法
    /// <summary>
    /// 首页点击匹配后，开始创建服务器和客户端
    /// </summary>
    public void PlayerConnected()
    {
        gameClient = new GameClient("127.0.0.1",8888);
        //判断客户端是否连接成功
        if (!gameClient.Start())
        {
            //失败，则关闭客户端开启服务器
            gameClient.Close();
            gameClient = null;
            gameServer = new GameServer("127.0.0.1", 8888);
            gameServer.Start();
            isServer = true;
        }
        else
        {
            isServer = false;
        }
    }
    /// <summary>
    /// 联网游戏界面点击开始
    /// </summary>
    public void BeReady()
    {
        if (isServer)
        {
            //通知对方我准备好了
            gameServer.SendMsg(new int[]{1,1,0,0,0,0});
            redHasReady = true;
        }
        else
        {
            //通知对方我准备好了
            gameClient.SendMsg(new int[] { 1, 2, 0, 0, 0, 0 });
            blackHasReady = true;
        }
        StartGame();
        if (netModeHasLoad)//之前游戏对象加载过，恢复默认UI布局
        {
            Replay();
        }
    }
    /// <summary>
    /// 双方准备完毕，开始游戏
    /// </summary>
    private void StartGame()
    {
        if (redHasReady && blackHasReady)//双方均准备完毕
        {
            redHasReady = blackHasReady = false;//置为原始状态，后续可以重新开始
            UIManager.Instance.CanClickButton(false);
            ResetGame();
        }
    }
    /// <summary>
    /// 认输，发送消息通知对方己方认输
    /// </summary>
    public void GiveUp()
    {
        gameOver = true;
        UIManager.Instance.ShowTip("对方胜利");
        if (isServer)
        {
            //通知对方我方认输
            gameServer.SendMsg(new int[]{3,1,0,0,0,0});
        }
        else
        {
            //通知对方我方认输
            gameClient.SendMsg(new int[]{3,0,0,0,0,0});
        }
        CloseSocket();//关闭Socket
        UIManager.Instance.netModeText.text = "匹配";
        UIManager.Instance.CanClickButton(true);

    }
    /// <summary>
    /// 解析后的编码转换为着法
    /// </summary>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    /// <param name="toX"></param>
    /// <param name="toY"></param>
    /// <returns></returns>
    private ChessReseting.ChessStep CodeToStep(int fromX, int fromY, int toX, int toY)
    {
        ChessReseting.ChessStep chessStepStep = new ChessReseting.ChessStep();
        GameObject gridOne = boardGrid[fromX, fromY];
        GameObject gridTwo = boardGrid[toX, toY];
        chessStepStep.gridOne = gridOne;
        chessStepStep.chessOneID = chessBoard[fromX, fromY];
        chessStepStep.gridTwo = gridTwo;
        chessStepStep.chessTwoID = chessBoard[toX, toY];
        chessStepStep.chessOne = gridOne.transform.GetChild(0).gameObject;
        if (gridTwo.transform.childCount != 0)
        {
            chessStepStep.chessTwo = gridTwo.transform.GetChild(0).gameObject;
        }
        return chessStepStep;
    }
    /// <summary>
    /// 解析接收到的游戏编码
    /// </summary>
    public void ParseGameCode()
    {
        int fromX, fromY, toX, toY;
        //游戏未结束，则移动棋子改变轮次
        int gameState = gameCodeReceive[0];//游戏状态
        int rivalColor = gameCodeReceive[1];//对方颜色 0为黑色 1为红色
        switch (gameState)
        {
            case 0://对方继续，解析对方的数据更新棋盘
                gameOver = false;
                fromX = gameCodeReceive[2];
                fromY = gameCodeReceive[3];
                toX = gameCodeReceive[4];
                toY = gameCodeReceive[5];
                movingOfChess.HaveAGoodMove(CodeToStep(fromX,fromY,toX,toY));
                redChessMove=!redChessMove;//更新轮次
                if (rivalColor==0)//对方黑色，走完轮到红色
                {
                    UIManager.Instance.ShowTip("红方走");
                }
                else//对方红色，走完轮到黑色
                {
                    UIManager.Instance.ShowTip("黑方走");
                }
                checkmate.JudgeIfCheckmate();
                if(gameOver)
                    CloseSocket();
                break;
            case 1://对方准备
                if (rivalColor==1)//对方红色
                {
                    redHasReady = true;
                    StartGame();
                }
                else
                {
                    blackHasReady = true;
                    StartGame();
                }
                break;
            case 2://连接成功
                System.Threading.Thread.Sleep(500);
                UIManager.Instance.StopAllCoroutines();//关闭倒计时
                BeReady();//进入准备状态
                UIManager.Instance.CanClickButton(false);
                UIManager.Instance.netModeText.text = "游戏中";
                UIManager.Instance.waitTime = 0;
                break;
            case 3://对方认输
                gameOver = true;
                CloseSocket();
                UIManager.Instance.ShowTip("对方认输");
                UIManager.Instance.netModeText.text = "匹配";
                UIManager.Instance.CanClickButton(true);//游戏结束可以重新开始
                break;
            default:
                break;
        }
    }

    public void CloseSocket()
    {
        gameServer?.Close();
        gameClient?.Close();
        gameServer = null;
        gameClient = null;
        Debug.Log("关闭Socket");
    }

    #endregion
}
