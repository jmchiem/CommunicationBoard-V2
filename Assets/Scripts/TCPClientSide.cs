using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

#if !UNITY_EDITOR
using System.Threading.Tasks;
#endif

public class PupilData
{
    public string topic;
    public float[] norm_pos = new float[2];
    public float confidence;
    public float timestamp;
    public PupilData()
    {
        norm_pos[0] = 0;
        norm_pos[1] = 0;
    }
}

public class TCPClientSide : MonoBehaviour
{
#if !UNITY_EDITOR
    private bool _useUWP = true;
    private Windows.Networking.Sockets.StreamSocket socket;
    private Task exchangeTask;
#endif

#if UNITY_EDITOR
    private bool _useUWP = false;
    System.Net.Sockets.TcpClient client;
    System.Net.Sockets.NetworkStream stream;
    private Thread exchangeThread;
#endif

    private Byte[] bytes = new Byte[256];
    private StreamWriter writer;
    private StreamReader reader;

    [SerializeField]
    private GameObject pupil1;
    [SerializeField]
    private GameObject pupil0;
    [SerializeField]
    private GameObject gaze;
    [SerializeField]
    private GameObject cursor;

    private float boardWidth;
    private float boardHeight;
    /////////////////////////////
    //private PupilData p0;
    /////////////////////////////
    private static int buffer_size = 1;
    private PupilData[,] buffer = new PupilData[2, buffer_size];
    private float[,] errorBuffer = new float[buffer_size, 2];
    private int buffer_counter = 0;

    public void Start()
    {
        boardWidth = Mathf.Abs(GlobalVars.p2x - GlobalVars.p1x);
        boardHeight = Mathf.Abs(GlobalVars.p3z - GlobalVars.p1z);
        Debug.Log("Height: " + boardHeight);
        Debug.Log("Width: " + boardWidth);
        Connect(GlobalVars.remoteIP, GlobalVars.remotePort);
    }

    public void Connect(string host, string port)
    {
        if (_useUWP)
        {
            ConnectUWP(host, port);
        }
        else
        {
            ConnectUnity(host, port);
        }
    }



#if UNITY_EDITOR
    private void ConnectUWP(string host, string port)
#else
    private async void ConnectUWP(string host, string port)
#endif
    {
#if UNITY_EDITOR
        errorStatus = "UWP TCP client used in Unity!";
#else
        try
        {
            if (exchangeTask != null) StopExchange();
        
            socket = new Windows.Networking.Sockets.StreamSocket();
            Windows.Networking.HostName serverHost = new Windows.Networking.HostName(host);
            await socket.ConnectAsync(serverHost, port);
        
            Stream streamOut = socket.OutputStream.AsStreamForWrite();
            writer = new StreamWriter(streamOut) { AutoFlush = true };
        
            Stream streamIn = socket.InputStream.AsStreamForRead();
            reader = new StreamReader(streamIn);

            RestartExchange();
            successStatus = "Connected!";
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }
#endif
    }

    private void ConnectUnity(string host, string port)
    {
#if !UNITY_EDITOR
        errorStatus = "Unity TCP client used in UWP!";
#else
        try
        {
            if (exchangeThread != null) StopExchange();

            client = new System.Net.Sockets.TcpClient(host, Int32.Parse(port));
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream) { AutoFlush = true };

            RestartExchange();
            successStatus = "Connected!";
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }
#endif
    }

    private bool exchanging = false;
    //////////////////////////////////////////////////////////
    public volatile bool exchangeStopRequested = false;
    //////////////////////////////////////////////////////////
    private string lastPacket = null;

    private string errorStatus = null;
    private string warningStatus = null;
    private string successStatus = null;
    private string unknownStatus = null;

    public void RestartExchange()
    {
#if UNITY_EDITOR
        if (exchangeThread != null) StopExchange();
        exchangeStopRequested = false;
        exchangeThread = new System.Threading.Thread(ExchangePackets);
        exchangeThread.Start();
#else
        if (exchangeTask != null) StopExchange();
        exchangeStopRequested = false;
        exchangeTask = Task.Run(() => ExchangePackets());
#endif
    }

    public void Update()
    {
        if (lastPacket != null)
        {
            ReportDataToTrackingManager(lastPacket);
        }

        if (errorStatus != null)
        {
            Debug.Log("Error: " + errorStatus);
            errorStatus = null;
        }
        if (warningStatus != null)
        {
            Debug.Log("Warning: " + warningStatus);
            warningStatus = null;
        }
        if (successStatus != null)
        {
            Debug.Log("Succeed: " + successStatus);
            successStatus = null;
        }
        if (unknownStatus != null)
        {
            Debug.Log("Unknown: " + unknownStatus);
            unknownStatus = null;
        }
    }

    public void ExchangePackets()
    {
        while (!exchangeStopRequested)
        {
            if (writer == null || reader == null) continue;
            exchanging = true;

            // writer.Write("request_pupil");
            // Debug.Log("Sent data!");
            string received = null;

#if UNITY_EDITOR
            byte[] bytes = new byte[client.SendBufferSize];
            int recv = 0;
            while (true)
            {
                recv = stream.Read(bytes, 0, client.SendBufferSize);
                received += Encoding.UTF8.GetString(bytes, 0, recv);
                if (received.EndsWith("\n")) break;
            }
#else
            received = reader.ReadLine();
#endif

            lastPacket = received;

            exchanging = false;
        }

        Debug.Log("thread closed");
    }

    private void ReportDataToTrackingManager(string rawdata)
    {
        if (rawdata == null)
        {
            Debug.Log("Received a frame but data was null");
            return;
        }
        String[] dataset = rawdata.Split('\n');

        ////////////////////////
        PupilData pupildata;
        PupilData p0 = null;
        PupilData p1 = null;
        ////////////////////////

        /*
        foreach (var data in dataset)
        {
            if (String.IsNullOrEmpty(data)) continue;
            PupilData pupildata = JsonConvert.DeserializeObject<PupilData>(data);
            if (preData == null && pupildata.topic.EndsWith(".1.")) continue;
            if (pupildata.topic.EndsWith(".0.")) preData = pupildata;
            else if (pupildata.confidence >= 0.60 && preData.confidence >= 0.60)
                //PushToBuffer(preData, pupildata);
                TrackPupil(preData, pupildata);
        }
        */
        //////////////////////////////////////////////////////////////////////////////////
        for (int i = 0; i < dataset.Length; i++)
        {
            if (String.IsNullOrEmpty(dataset[i]))
                continue;

            pupildata = JsonConvert.DeserializeObject<PupilData>(dataset[i]);

            if (pupildata.topic.EndsWith(".0."))
            {
                p0 = pupildata;

                if (p1 != null)
                    continue;

                else if (p0.confidence >= 0.60)
                    TrackPupil2(p0, true);
                    
            }
            else if (pupildata.topic.EndsWith(".1."))
            { 
                p1 = pupildata;

                if (p0 != null)
                {
                    if (p0.confidence >= 0.60 && p1.confidence >= 0.60)
                        TrackPupil(p0, p1);

                    continue;
                }
                else if (p1.confidence >= 0.60)
                    TrackPupil2(p1, false);
                 
            }

        }

    }
    //////////////////////////////////////////////////////////////////////////////////

    private void PushToBuffer(PupilData p0data, PupilData p1data)
    {
        if (buffer_counter < buffer_size)
        {
            buffer[0, buffer_counter] = p0data;
            buffer[1, buffer_counter] = p1data;
            errorBuffer[buffer_counter, 0] = cursor.transform.position.x;
            errorBuffer[buffer_counter, 1] = cursor.transform.position.y;
            buffer_counter++;
        }
        else
        {
            PupilData p0 = new PupilData();
            PupilData p1 = new PupilData();
            float ex = 0, ey = 0;
            for (int i = 0; i < buffer_size; i++)
            {
                p0.norm_pos[0] += buffer[0, i].norm_pos[0];
                p0.norm_pos[1] += buffer[0, i].norm_pos[1];
                p1.norm_pos[0] += buffer[1, i].norm_pos[0];
                p1.norm_pos[1] += buffer[1, i].norm_pos[1];
                ex += errorBuffer[i, 0];
                ey += errorBuffer[i, 1];
            }
            p0.norm_pos[0] /= (float)buffer_size;
            p0.norm_pos[1] /= (float)buffer_size;
            p1.norm_pos[0] /= (float)buffer_size;
            p1.norm_pos[1] /= (float)buffer_size;
            ex /= (float)buffer_size;
            ey /= (float)buffer_size;
            //TrackPupil(p0, p1, ex, ey);
            buffer_counter = 0;
        }
    }

    //private void TrackPupil(PupilData p0data, PupilData p1data, float ex, float ey)
    private void TrackPupil(PupilData p0data, PupilData p1data)
    {
        float nx = -p1data.norm_pos[0] + 0.5f;
        float nz = -p1data.norm_pos[1] + 0.5f;
        nx *= boardWidth;
        nz *= boardHeight;
        float tx = p0data.norm_pos[0] - 0.5f;
        float tz = p0data.norm_pos[1] - 0.5f;
        tx *= boardWidth;
        tz *= boardHeight;
        nx = GlobalVars.k01 * nx + GlobalVars.k02 * nz + GlobalVars.k03;
        nz = GlobalVars.k04 * nx + GlobalVars.k05 * nz + GlobalVars.k06;
        tx = GlobalVars.k11 * tx + GlobalVars.k12 * tz + GlobalVars.k13;
        tz = GlobalVars.k14 * tx + GlobalVars.k15 * tz + GlobalVars.k16;
        float gx = (tx + nx) / 2f;
        float gz = (tz + nz) / 2f;
        float y = -0.05f;
        //nx -= ex; nz -= ey;
        //tx -= ex; tz -= ey;
        //gx -= ex; tz -= ey;
        pupil0.transform.localPosition = new Vector3(nx, y, nz);
        pupil1.transform.localPosition = new Vector3(tx, y, tz);
        gaze.transform.localPosition = new Vector3(gx, y, gz);
        Debug.Log("P0 | x: " + nx + " z: " + nz);
        Debug.Log("P1 | x: " + tx + " z: " + tz);
        Debug.Log("Gaze | x: " + gx + " z: " + gz);
    }

    //////////////////////////////////////////////////////////////////////////////////
    // This method follows the logic of TrackPupil()
    // Since the position of p0 is used to calculate p1 and vice versa, 
    // if p0 is available, then technically the position of p1 is calculated
    // however with one camera p0 <=> p1 so this distinction isn't significant
    
    public void TrackPupil2(PupilData p, bool zero)
    {
        float x;
        float z;
        float y = -0.05f; 

        if (zero)
        {
            x = p.norm_pos[0] - 0.5f;
            z = p.norm_pos[1] - 0.5f;
            x *= boardWidth;
            z *= boardHeight;
            x = GlobalVars.k11 * x + GlobalVars.k12 * z + GlobalVars.k13;
            z = GlobalVars.k14 * x + GlobalVars.k15 * z + GlobalVars.k16;

            pupil1.transform.localPosition = new Vector3(x, y, z);
        }
        else
        {
            x = -p.norm_pos[0] + 0.5f;
            z = -p.norm_pos[1] + 0.5f;
            x *= boardWidth;
            z *= boardHeight;
            x = GlobalVars.k01 * x + GlobalVars.k02 * z + GlobalVars.k03;
            z = GlobalVars.k04 * x + GlobalVars.k05 * z + GlobalVars.k06;

            pupil0.transform.localPosition = new Vector3(x, y, z);
        }
        gaze.transform.localPosition = new Vector3(x, y, z);
        Debug.Log("Gaze | x: " + x + " z: " + z);
    }
    //////////////////////////////////////////////////////////////////////////////////

    public void StopExchange()
    {
        exchangeStopRequested = true;

#if UNITY_EDITOR
        if (exchangeThread != null)
        {
            exchangeThread.Abort();
            stream.Close();
            client.Close();
            writer.Close();
            reader.Close();

            stream = null;
            exchangeThread = null;
        }
#else
        if (exchangeTask != null) {
            exchangeTask.Wait();
            if(socket != null)
                socket.Dispose();
            //if(writer != null)
            //    writer.Dispose();
            //if(reader != null)
            //    reader.Dispose();

            socket = null;
            exchangeTask = null;
        }
#endif
        writer = null;
        reader = null;

        Debug.Log("stopping exchange");
    }

    public void OnDisable()
    {
        StopExchange();
    }

    public void OnDestroy()
    {
        StopExchange();
    }

}
 