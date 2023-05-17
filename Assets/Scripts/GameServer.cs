using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
/// <summary>
/// 服务器端
/// </summary>
public class GameServer
{
    //当前服务器监听子线程
    private Thread connectThread;
    //ip地址
    private string address;
    //端口号
    private int port;
    //远程客户端，连接成功时获取客户端对象，从而通讯
    private TcpClient remoteClient;
    private TcpListener tcpListener;
    private NetworkStream stream;
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="address">ip</param>
    /// <param name="port">端口号</param>
    public GameServer(string address,int port)
    {
        this.address = address;
        this.port = port;
    }
    /// <summary>
    /// 初始化服务器段套接字对象并开始监听
    /// </summary>
    private void InitServerSocket()
    {
        //缓冲区大小
        int bufferSize = 1 << 10;
        IPAddress ip = IPAddress.Parse(address);
        //创建监听器
        tcpListener = new TcpListener(ip, port);
        //开启监听器
        tcpListener.Start();
        Debug.Log("服务器开始监听");
        //如果有远程客户端连接，此时就会得到一个对象用于通讯
        remoteClient = tcpListener.AcceptTcpClient();
        Debug.Log("与客户端连接成功");
        // 通知对方连接成功，开始游戏
        SendMsg(new int[]{2,0,0,0,0,0});
        while (true)
        {
            try
            {
                //获取信息流
                stream = remoteClient.GetStream();
                byte[] buffer = new byte[bufferSize];
                //信息流读取到缓冲区
                int len = stream.Read(buffer,0,bufferSize);
                if (len == 0)
                {
                    Debug.Log("与客户端断开");
                    break;
                }
                //具体处理接收到的数据
                int[] result = BytesToInt(buffer, 0);
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
                Debug.Log("客户段异常"+e.Message);
                Close();
            }
        }
    }
    /// <summary>
    /// 启动服务器
    /// </summary>
    public void Start()
    {
        connectThread = new Thread(InitServerSocket);
        connectThread.Start();
    }
    /// <summary>
    /// 关闭服务器
    /// </summary>
    public void Close()
    {
        try
        {
            tcpListener?.Stop();//关闭监听器
            remoteClient?.Client.Shutdown(SocketShutdown.Both);//关闭Socket
            remoteClient?.Close();
            stream?.Close();//关闭数据流
            connectThread?.Abort();//关闭线程
            System.Threading.Thread.Sleep(100);
            Debug.Log("服务器线程关闭");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    /// <summary>
    /// 服务器端发送信息到客户端
    /// </summary>
    public void SendMsg(int[] gameCode)
    {
        remoteClient?.Client.Send(IntToBytes(gameCode,0));
    }

    /// <summary>
    /// int[]转byte[]采用小段表示法
    /// </summary>
    /// <param name="src"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public byte[] IntToBytes(int[] src,int offset)
    {
        byte[] values = new byte[src.Length * 4];
        for (int i = 0; i < src.Length; ++i)
        {
            values[offset] = (byte) src[i];
            values[offset + 1] = (byte) (src[i] >> 8);
            values[offset + 2] = (byte) (src[i] >> 16);
            values[offset + 3] = (byte) (src[i] >> 24);
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
    public int[] BytesToInt(byte[] src,int offset)
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
