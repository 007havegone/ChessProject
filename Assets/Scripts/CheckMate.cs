using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 检测是否将军的类
/// </summary>
public class Checkmate
{
    private GameManager gameManager;
    private UIManager uiManager;
    private int jiangX, jiangY, shuaiX, shuaiY;
    public Checkmate()
    {
        gameManager = GameManager.Instance;
        uiManager = UIManager.Instance;
    }
    /// <summary>
    /// 是否将军的检测方法
    /// 如果帅或将不存在棋盘上，UI显示结果，结束游戏
    /// 如果构成将军UI显示
    /// </summary>
    public void JudgeIfCheckmate()
    {
        GetKingPosition();
        if (gameManager.chessBoard[jiangX, jiangY] != 1)//未找到将军
        {
            uiManager.ShowTip("红方胜");
            AudioSourceManager.Instance.PlaySound(6);
            if (gameManager.chessPeople == 3)
            {
                UIManager.Instance.netModeText.text = "匹配";
                UIManager.Instance.CanClickButton(true);//可以重新开始游戏
            }
            gameManager.gameOver = true;
            return;
        }
        if (gameManager.chessBoard[shuaiX, shuaiY] != 8)
        {
            uiManager.ShowTip("黑方胜");
            if (gameManager.chessPeople == 1)//AI胜利，玩家失败
            {
                AudioSourceManager.Instance.PlaySound(7);
            }
            else if (gameManager.chessPeople==2)
            {
                AudioSourceManager.Instance.PlaySound(6);
            }
            else//联网模式
            {
                if (gameManager.isServer)//对方胜利
                {
                    AudioSourceManager.Instance.PlaySound(7);
                }
                else
                {
                    AudioSourceManager.Instance.PlaySound(6);
                }
            }
            if (gameManager.chessPeople == 3)
            {
                UIManager.Instance.netModeText.text = "匹配";
                UIManager.Instance.CanClickButton(true);//可以重新开始游戏
            }
            gameManager.gameOver = true;
            return;
        }
        //当前是否将军
        bool ifCheckmate;
        // 扫描棋盘每颗棋子判断是否可以构成将军
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                switch (gameManager.chessBoard[i,j])
                {
                    
                    case 2:
                        ifCheckmate = gameManager.rules.IsValidMove(gameManager.chessBoard, i, j, shuaiX, shuaiY);
                        if (ifCheckmate)
                        {
                            AudioSourceManager.Instance.PlaySound(4);
                            uiManager.ShowTip("帅被車将军");
                        }
                        break;
                    case 3:
                        ifCheckmate = gameManager.rules.IsValidMove(gameManager.chessBoard, i, j, shuaiX, shuaiY);
                        if (ifCheckmate)
                        {
                            AudioSourceManager.Instance.PlaySound(4);
                            uiManager.ShowTip("帅被马将军");
                        }
                        break;
                    case 4:
                        ifCheckmate = gameManager.rules.IsValidMove(gameManager.chessBoard, i, j, shuaiX, shuaiY);
                        if (ifCheckmate)
                        {
                            AudioSourceManager.Instance.PlaySound(4);
                            uiManager.ShowTip("帅被炮将军");
                        }
                        break;
                    case 7:
                        ifCheckmate = gameManager.rules.IsValidMove(gameManager.chessBoard, i, j, shuaiX, shuaiY);
                        if (ifCheckmate)
                        {
                            AudioSourceManager.Instance.PlaySound(4);
                            uiManager.ShowTip("帅被卒将军");
                        }
                        break;
                    case 9:
                        ifCheckmate = gameManager.rules.IsValidMove(gameManager.chessBoard, i, j, jiangX, jiangY);
                        if (ifCheckmate)
                        {
                            AudioSourceManager.Instance.PlaySound(4);
                            uiManager.ShowTip("将被車将军");
                        }
                        break;
                    case 10:
                        ifCheckmate = gameManager.rules.IsValidMove(gameManager.chessBoard, i, j, jiangX, jiangY);
                        if (ifCheckmate)
                        {
                            AudioSourceManager.Instance.PlaySound(4);
                            uiManager.ShowTip("将被马将军");
                        }
                        break;
                    case 11:
                        ifCheckmate = gameManager.rules.IsValidMove(gameManager.chessBoard, i, j, jiangX, jiangY);
                        if (ifCheckmate)
                        {
                            AudioSourceManager.Instance.PlaySound(4);
                            uiManager.ShowTip("将被炮将军");
                        }
                        break;
                    case 14:
                        ifCheckmate = gameManager.rules.IsValidMove(gameManager.chessBoard, i, j, jiangX, jiangY);
                        if (ifCheckmate)
                        {
                            AudioSourceManager.Instance.PlaySound(4);
                            uiManager.ShowTip("将被兵将军");
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
    /// <summary>
    /// 扫描chessBoard，获取将帅坐标位置坐标
    /// </summary>
    private void GetKingPosition()
    {
        
        for (int i = 0; i < 3; i++)
        {
            for (int j = 3; j < 6; j++)
                if (gameManager.chessBoard[i, j] == 1)
                {
                    jiangX = i;
                    jiangY = j;
                }

        }
        for (int i = 7; i < 10; i++)
        {
            for (int j = 3; j < 6; j++)
            {
                if (gameManager.chessBoard[i, j] == 8)
                {
                    shuaiX = i;
                    shuaiY = j;
                }
            }
        }
    }
}
