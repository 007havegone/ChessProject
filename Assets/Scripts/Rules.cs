using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework.Internal;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// 棋子的规则类
/// </summary>
public class Rules
{
    /// <summary>
    /// 检测当前此次移动是否合法
    /// 判断是否异边，随后调用IsValid判断是否在合法移动范围内
    /// </summary>
    /// <param name="position">当前棋盘状况,其实可以不加,在GameManager中可以取到</param>
    /// <param name="fromX">起始X位置</param>
    /// <param name="fromY">起始Y位置</param>
    /// <param name="toX">目的X位置</param>
    /// <param name="toY">目的Y位置</param>
    /// <returns></returns>
    public bool IsValidMove(int[,]position,int fromX,int fromY,int toX,int toY)
    {
        int moveChessID = position[fromX, fromY];
        int targetID = position[toX, toY];
        if (IsSameSide(moveChessID, targetID))
        {
            return false;
        }
        return IsValid(moveChessID,position,fromX,fromY,toX,toY);
    }
    /// <summary>
    /// 判断选中的两个游戏物体是否是同方棋子
    /// </summary>
    /// <returns></returns>
    public bool IsSameSide(int id1,int id2)
    {
        if (IsBlack(id1) && IsBlack(id2) || IsRed(id1) && IsRed(id2))
            return true;
        else
            return false;
    }
    /// <summary>
    /// 判断当前游戏物体是否黑棋
    /// </summary>
    /// <param name="id">棋子id</param>
    /// <returns></returns>
    public bool IsBlack(int id)
    {
        if (id > 0 && id < 8)
            return true;
        else
            return false;
    }
    /// <summary>
    /// 判断当前游戏物体是否红棋
    /// </summary>
    /// <param name="id">棋子id</param>
    /// <returns></returns>
    public bool IsRed(int id)
    {
        if (id >= 8 && id <= 14)
        {
            return true;
        }
        else
            return false;
    }
    /// <summary>
    /// 所有种类棋子的走法是否合法
    /// </summary>
    /// <param name="moveChessID">棋子id</param>
    /// <param name="position">棋盘的状况</param>
    /// <param name="fromX">起始X位置</param>
    /// <param name="fromY">起始Y位置</param>
    /// <param name="toX">目标X位置</param>
    /// <param name="toY">目标Y位置</param>
    public bool IsValid(int moveChessID,int [,]position,int fromX,int fromY,int toX,int toY)
    {
        //位置相同非法
        if (fromX == toX && fromY == toY)
            return false;
        //将帅同一直线构成KingKill
        if (KingKill(position, fromX, fromY, toX, toY))
        {
            return false;
        }
        int i = 0, j = 0;
        switch (moveChessID)
        {
            //分红黑方处理的情况
            case 1://黑将
                if (toX > 2 || toY > 5 || toY < 3)//出九宫格
                {
                    return false;
                }
                if (Mathf.Abs(fromX - toX) + Mathf.Abs(fromY - toY) > 1)//跨越大于1
                {
                    return false;
                }
                break;
            case 8://红帅
                if (toX < 7 || toY > 5 || toY < 3)//出九宫格
                {
                    return false;
                }
                if (Mathf.Abs(fromX - toX) + Mathf.Abs(fromY - toY) > 1)//跨越大于1
                {
                    return false;
                }
                break;
            case 5://黑士
                if (toX > 2 || toY > 5 || toY < 3)//出九宫格
                {
                    return false;
                }

                if (Mathf.Abs(fromX - toX) != 1 || Mathf.Abs(fromY - toY) != 1)//不是斜线
                {
                    return false;
                }
                break;
            case 12://红仕
                if (toX < 7 || toY > 5 || toY < 3)//出九宫格
                {
                    return false;
                }
                if (Mathf.Abs(fromX - toX) != 1 || Mathf.Abs(fromY - toY) != 1)//不是斜线
                {
                    return false;
                }
                break;
            case 6://黑象
                if (toX > 4)//过河
                    return false;
                if (Mathf.Abs(fromX - toX) != 2 || Mathf.Abs(fromY - toY) != 2)//不是田字
                    return false;
                if (position[(fromX + toX) / 2, (fromY + toY) / 2] != 0)//塞象眼
                    return false;
                break;
            case 13://红相
                if (toX < 5)
                    return false;
                if (Mathf.Abs(fromX - toX) != 2 || Mathf.Abs(fromY - toY) != 2)//不是田字
                    return false;
                if (position[(fromX + toX) / 2, (fromY + toY) / 2] != 0)//塞象眼
                    return false;
                break;
            case 7://黑卒
                //不能回头
                if (toX < fromX)
                    return false;
                //过河前只能走直线
                if (fromX < 5 && fromX == toX)
                    return false;
                //兵只能走一格
                if (toX - fromX + Mathf.Abs(toY - fromY) > 1)
                    return false;
                break;
            case 14://红兵
                //不能回头
                if (toX > fromX)
                    return false;
                //过河前智能直走
                if (fromX > 4 && fromX == toX)
                    return false;
                //兵智能走一格
                if (fromX - toX + Mathf.Abs(toY - fromY) > 1)
                    return false;
                break;
            //不分红黑方处理的情况
            case 2://红黑车
            case 9:
                //不是走直线
                if (fromX != toX && fromY != toY)
                    return false;
                //判断移动的直线上是否有棋子
                if (fromX != toX)//竖线移动
                {
                    if (fromX < toX)//向下移动
                    {
                        for (i = fromX+1; i < toX; i++)
                        {
                            if (position[i, fromY] != 0)
                                return false;
                        }
                    }
                    else//向上移动
                    {
                        for (i = toX+1; i < fromX; i++)
                        {
                            if (position[i, fromY] != 0)
                                return false;
                        }
                    }
                }
                else//横线移动
                {
                    if (fromY < toY)//向右移动
                    {
                        for (j= fromY+1; j < toY; j++)
                        {
                            if (position[fromX, j] != 0)
                                return false;
                        }
                    }
                    else//向左移动
                    {
                        for (j= toY + 1; j < fromY; ++j)
                        {
                            if (position[fromX, j] != 0)
                                return false;
                        }
                    }
                }
                break;
            case 3://红黑马
            case 10:
                //横日
                if (!(Mathf.Abs(fromX - toX) == 1 && Mathf.Abs(fromY - toY) == 2 ||
                      //竖日
                      Mathf.Abs(fromX - toX) == 2 && Mathf.Abs(fromY - toY) == 1))
                {
                    return false;
                }
                //竖日蹩马腿
                if (Mathf.Abs(fromX - toX) == 2 && position[(fromX + toX) / 2, fromY] != 0)
                    return false;
                //横日蹩马腿
                if (Mathf.Abs(fromY - toY) == 2 && position[fromX, (fromY + toY) / 2] != 0)
                    return false;
                break;
            case 4://红黑炮
            case 11:
                //不是直线
                if (fromX != toX && fromY != toY)
                {
                    return false;
                }
                    
                int count = 0;
                //计算移动路径多少个棋子
                if (fromX != toX)//竖线移动
                {
                    if (fromX < toX)//向下移动
                    {
                        for (i = fromX + 1; i < toX; i++)
                        {
                            if (position[i, fromY] != 0)
                                count++;
                        }
                    }
                    else//向上移动
                    {
                        for (i = toX + 1; i < fromX; i++)
                        {
                            if (position[i, fromY] != 0)
                                count++;
                        }
                    }
                }
                else//横线移动
                {
                    if (fromY < toY)//向右移动
                    {
                        for (j = fromY + 1; j < toY; j++)
                        {
                            if (position[fromX, j] != 0)
                                count++;
                        }
                    }
                    else//向左移动
                    {
                        for (j = toY + 1; j < fromY; j++)
                        {
                            if (position[fromX, j] != 0)
                                count++;
                        }
                    }
                }
                //炮移动，路径上有棋子
                if (position[toX, toY] == 0 && count>0)
                {
                    return false;
                }
                //炮翻山吃子 
                if (position[toX, toY] > 0 && count != 1)
                {
                    return false;
                }
                return true;
        }
        return true;
    }
    /// <summary>
    /// 判断将帅是否在同一直线上，kingKill返回true，否则false
    /// </summary>
    /// <param name="position">棋盘的状况</param>
    /// <param name="fromX">起始X位置</param>
    /// <param name="fromY">起始Y位置</param>
    /// <param name="toX">目标X位置</param>
    /// <param name="toY">目标Y位置</param>
    /// <returns></returns>
    public bool KingKill(int[,] position,int fromX,int fromY,int toX,int toY)
    {
        int jiangX = 0, jiangY = 0, shuaiX = 0, shuaiY = 0;
        int count = 0; 
        //拷贝一份棋盘状况，随后假设移动进行合法性判断
        int[,] tempPosition = new int[10, 9];
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                tempPosition[i, j] = position[i, j];
            }
        }
        // 进行移动，判断之后的情况
        tempPosition[toX, toY] = tempPosition[fromX, fromY];
        tempPosition[fromX, fromY] = 0;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 3; j < 6; j++)
            {
                if (tempPosition[i, j] == 1)
                {
                    jiangX = i;
                    jiangY = j;
                }
            }
        }
        for (int i = 7; i < 10; i++)
        {
            for (int j = 3; j < 6; j++)
            {
                if (tempPosition[i, j] == 8)
                {
                    shuaiX = i;
                    shuaiY = j;
                }
            }
        }

        if (shuaiY == jiangY)//同一直线上
        {
            for (int i = jiangX + 1; i < shuaiX; ++i)
            {
                if (tempPosition[i, jiangY] != 0)
                    count++;
            }
        }
        else//不在同一直线上
        {
            count = -1;
        }
        if (count == 0)//不合法
            return true;
        return false;
    }
}
