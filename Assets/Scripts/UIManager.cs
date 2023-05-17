using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 页面之间显示与跳转，按钮的触发方法，在GameManager之后实例化
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // 0.主菜单 1.单机 2.模式选择 3.难度选择 4.单机游戏 5.联网游戏
    public enum PanelID
    {
        Main,
        Standalone,
        ModelOption,
        LevelOption,
        LocalGame,
        NetworkGame
    };
    public GameObject[] panels;

    public Text tipUIText;//当前使用的文本提示UI
    public Text[] tipUITexts;//两个对应提示UI的引用 0.单机 1联网
    private GameManager gameManager;

    public Button netModePlayButton;//匹配按钮
    public Button giveUpButton;//放弃按钮
    public Text netModeText;//匹配按钮文本
    // Start is called before the first frame update
    public int waitTime = 0;//倒计时
    void Start()
    {
        Debug.Log("UIManager Start初始化");
        Instance = this;
        gameManager = GameManager.Instance;
    }

    #region 页面跳转
    /// <summary>
    /// 单机模式
    /// </summary>
    public void StandaloneMode()
    {
        Debug.Log("点击Standalone Mode");
        panels[(int) PanelID.Main].SetActive(false);
        panels[(int)PanelID.Standalone].SetActive(true);
    }
    /// <summary>
    /// 联网模式
    /// </summary>
    public void NetWorkingMode()
    {
        Debug.Log("点击NetWorking Mode");
        panels[(int)PanelID.Main].SetActive(false);
        panels[(int)PanelID.NetworkGame].SetActive(true);
        gameManager.chessPeople = 3;
        UIManager.Instance.CanClickButton(true);
        tipUIText = tipUITexts[1];
    }
    /// <summary>
    /// 退出游戏
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("点击退出游戏");
        Application.Quit();
    }
    /// <summary>
    /// 人机模式
    /// </summary>
    public void PVEMode()
    {
        Debug.Log("PVE模式");
        gameManager.chessPeople = 1;
        panels[(int)PanelID.ModelOption].SetActive(false);
        panels[(int)PanelID.LevelOption].SetActive(true);
    }
    /// <summary>
    /// 双人模式
    /// </summary>

    public void PVPMode()
    {
        Debug.Log("PVP模式");
        tipUIText = tipUITexts[0];
        gameManager.chessPeople = 2;
        LoadGame();

    }

    public void LevelOption(int level)
    {
        Debug.Log("选择难度");
        gameManager.currentLevel = level;
        tipUIText = tipUITexts[0];
        LoadGame();
    }
    #endregion
    #region 加载游戏
    /// <summary>
    /// ResetGame重置数据，恢复默认UI，显示游戏主界面
    /// </summary>
    private void LoadGame()
    {
        gameManager.ResetGame();
        SetUI();
        panels[(int)PanelID.LocalGame].SetActive(true);
    }
    /// <summary>
    /// 恢复进入游戏时的默认UI显示
    /// </summary>
    private void SetUI()
    {
        panels[(int)PanelID.ModelOption].SetActive(true);
        panels[(int)PanelID.LevelOption].SetActive(false);
        panels[(int)PanelID.Standalone].SetActive(false);
        panels[(int)PanelID.Main].SetActive(true);
    }
    #endregion
    #region 游戏中的UI方法
    /// <summary>
    /// 悔棋
    /// </summary>
    public void UnDo()
    {
        Debug.Log("悔棋");
        gameManager.chessResting.ResetChess();
    }
    /// <summary>
    /// 重玩
    /// </summary>
    public void Replay()
    {
        Debug.Log("重玩");
        gameManager.Replay();
    }
    /// <summary>
    /// 返回
    /// </summary>
    public void ReturnToMain()
    {
        Debug.Log("返回菜单");
        panels[(int)PanelID.LocalGame].SetActive(false);
        gameManager.Replay();//重置UI和游戏中数据
        gameManager.gameOver = true;
    }

    public void NetReturnToMain()
    {
        Debug.Log("返回菜单");
        if (!gameManager.gameOver)//还未结束退出游戏等于放弃
            GiveUp();
        panels[(int)PanelID.NetworkGame].SetActive(false);
        StopAllCoroutines();//关闭倒计时协程
        gameManager.CloseSocket();
        gameManager.Replay();
        SetUI();//还原UI
        gameManager.gameOver = true;
    }
    /// <summary>
    /// 轮次以及将军信息的提示
    /// </summary>
    public void ShowTip(string str)
    {
        tipUIText.text = str;
    }
    /// <summary>
    /// 开始联网匹配
    /// </summary>
    public void StartNetWorkingMode()
    {
        Debug.Log("开始联网匹配");
        if (waitTime == 0)
        {
            gameManager.PlayerConnected();//启动Socket联网
            //启动协程更新倒计时
            StartCoroutine(UpdateStartButtonText(30));
        }
        else//再次点击，取消匹配
        {
            StopAllCoroutines();//关闭现有计时器
            gameManager.CloseSocket();//关闭socket
            waitTime = 0;
            netModeText.text = "匹配";
        }
    }

    public void GiveUp()
    {
        Debug.Log("认输");
        gameManager.GiveUp();
    }
    /// <summary>
    /// 匹配按钮和放弃按钮相反
    /// </summary>
    /// <param name="canClick"></param>
    public void CanClickButton(bool canClick)
    {
        Debug.Log("设置按钮");
        netModePlayButton.interactable = canClick;
        giveUpButton.interactable = !canClick;
    }
    /// <summary>
    /// 倒计时定时器（协程）
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    IEnumerator UpdateStartButtonText(int time)
    {
        waitTime = time;
        while (waitTime > 0)
        {
            netModeText.text = waitTime.ToString();
            waitTime -= 1;
            yield return new WaitForSeconds(1.0f);
        }
        //匹配失败关闭Socket
        gameManager.CloseSocket();
        netModeText.text = "匹配";
    }
    #endregion

}
