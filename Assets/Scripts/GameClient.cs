using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class GameClient
{
    //服务器地址
    private string serverAddress;
    //服务器端口
    private int port;
    //当前tcp客户端
    private TcpClient localClient;
    //接受服务器信息的进程
    private Thread receivedThread;
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serverAddress">服务器IP</param>
    /// <param name="port">端口号</param>
    public GameClient(string serverAddress,int port)
    {
        this.serverAddress = serverAddress;
        this.port = port;
    }
    /// <summary>
    /// 关闭客户端
    /// </summary>
    public void Close()
    {
        localClient?.Close();
        receivedThread?.Abort();
        System.Threading.Thread.Sleep(100);
        Debug.Log("客户端线程关闭完成");
    }
    /// <summary>
    /// 启动客户端，连接服务器
    /// </summary>
    /// <returns></returns>
    public bool Start()
    {
        localClient = new TcpClient();
        try
        {
            localClient.Connect(IPAddress.Parse(serverAddress),port);
            //创建连接线程并启动
            receivedThread = new Thread(SocketReceive);
            receivedThread.Start();
            Debug.Log("客户端线程启动");
        }
        catch (Exception e)
        {
            Debug.Log("客户端连接服务器异常："+e.Message);
            if (!localClient.Connected)
            {
                return false;
            }
        }
        // 通知对方连接成功后
        SendMsg(new int[] { 2, 0, 0, 0, 0, 0 });
        return true;
    }

    /// <summary>
    /// 客户端检测接受的服务器消息
    /// </summary>
    private void SocketReceive()
    {
        // 成功连接
        if (localClient != null)
        {
            int bufferSize = 1 << 10;
            //消息缓冲区
            byte[] resultBuffer = new byte[bufferSize];
            while (true)
            {
                try
                {
                    int len = localClient.Client.Receive(resultBuffer);
                    if (len == 0)
                    {
                        Debug.Log("与服务器的连接中断");
                        break;
                    }
                    //处理接受的数据
                    int[] result = BytesToInt(resultBuffer, 0);
                    for (int i = 0; i < GameManager.Instance.gameCodeReceive.Length; i++)
                    {
                        GameManager.Instance.gameCodeReceive[i] = result[i];
                    }
                    // 由于Unity线程不支持使用UnityEngine的API，可以使用UnityEngine定义的基本类型的函数
                    // 所以需要使用一个标志来通知主线程消息到达。
                    GameManager.Instance.isReceived = true;
                }
                catch (Exception e)
                {
                    Debug.Log("客户端接受数据异常："+e.Message);
                    Close();
                }
            }
        }
    }
    /// <summary>
    /// 客户端发送信息
    /// </summary>
    public void SendMsg(int[] gameCode)
    {
        localClient?.Client.Send(IntToBytes(gameCode,0));
    }
    /// <summary>
    /// int[]转byte[]采用小段表示法
    /// </summary>
    /// <param name="src"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public byte[] IntToBytes(int[] src, int offset)
    {
        byte[] values = new byte[src.Length * 4];
        for (int i = 0; i < src.Length; ++i)
        {
            values[offset] = (byte)src[i];
            values[offset + 1] = (byte)(src[i] >> 8);
            values[offset + 2] = (byte)(src[i] >> 16);
            values[offset + 3] = (byte)(src[i] >> 24);
            offset += 4;
        }
        return values;
    }
    /// <summary>
    /// 将byte[]转int[]采用小段表示法
    /// </summary>
    /// <param name="src"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public int[] BytesToInt(byte[] src, int offset)
    {
        int[] values = new int[src.Length / 4];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = src[offset] | src[offset + 1] << 8 | src[offset + 2] << 16 | src[offset + 3] << 24;
            offset += 4;
        }
        return values;

    }
}
