// setting initial position of pupils in global variables

using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;

#if !UNITY_EDITOR
using System.Threading.Tasks;
#endif

public class Calibration : MonoBehaviour, IInputClickHandler
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
    private GameObject point1;
    [SerializeField]
    private GameObject point2;
    [SerializeField]
    private GameObject point3;
    [SerializeField]
    private GameObject point4;
    [SerializeField]
    private GameObject activateObject;
    [SerializeField]
    private GameObject cursor;

    private int clickCounter;
    private int dataSize0, dataSize1;
    private bool clickable;
    private static int sizeGoal = 100;
    int skipSize = 20;
    private float boardWidth;
    private float boardHeight;
    private PupilData[] pupil0Data = new PupilData[sizeGoal + 5];
    private PupilData[] pupil1Data = new PupilData[sizeGoal + 5];
    private float[] o0x = new float[5];
    private float[] o0y = new float[5];
    private float[] o1x = new float[5];
    private float[] o1y = new float[5];
    private float[] rx = new float[5];
    private float[] ry = new float[5];
    private float basex;
    private float basey;
    private float[] errorx0 = new float[sizeGoal + 5];
    private float[] errorx1 = new float[sizeGoal + 5];
    private float[] errory0 = new float[sizeGoal + 5];
    private float[] errory1 = new float[sizeGoal + 5];

    void IInputClickHandler.OnInputClicked(InputClickedEventData eventData)
    {
        //Debug.Log("Start calibration...");
        if (clickable)
        {
            if (clickCounter < 4)
            {
                if(clickCounter == 0)
                {
                    //basex = cursor.transform.position.x;
                    //basey = cursor.transform.position.y;
                    basex = 0f;
                    basey = 0f;
                }
                clickable = false;
                clickCounter++;
                GameObject textObject = GameObject.Find("Description");
                textObject.GetComponent<TextMesh>().text = "Calibrating Point " + clickCounter + "...";
            }
            else
            {
                activateObject.SetActive(true);
                gameObject.SetActive(false);  // not sure what this does
                ////////////////////////////////////////////////////////////////////////
                StopExchange(); // why not disable the calibration plane?
                ////////////////////////////////////////////////////////////////////////
            }
        }
        //Debug.Log(clickCounter);
    }

    // Use this for initialization
    void Start () {
        rx[1] = GlobalVars.p1x; ry[1] = GlobalVars.p1z;
        rx[2] = GlobalVars.p2x; ry[2] = GlobalVars.p2z;
        rx[3] = GlobalVars.p3x; ry[3] = GlobalVars.p3z;
        rx[4] = GlobalVars.p4x; ry[4] = GlobalVars.p4z;
    }

    void OnEnable()
    {
        clickCounter = 0;
        clickable = true;
        dataSize0 = 0;
        dataSize1 = 0;
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

    public void ExchangePackets()
    {
        while (!exchangeStopRequested)
        {
            if (writer == null || reader == null) continue;
            exchanging = true;

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
            // Debug.Log(lastPacket);
            exchanging = false;
        }
    }

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
    }

    public void OnDisable()
    {
        StopExchange();
    }

    public void OnDestroy()
    {
        StopExchange();
    }

    void CollectCalibrationDataAtScene(int scene)
    {
        GameObject point;
        switch(scene)
        {
            case 1: point = point1; break;
            case 2: point = point2; break;
            case 3: point = point3; break;
            case 4: point = point4; break;
            default: point = point1; break;
        }

        string rawdata = lastPacket;
        if (rawdata == null)
        {
            Debug.Log("Received a frame but data was null???");
            return;
        }
        String[] dataset = rawdata.Split('\n');
        foreach (var data in dataset)
        {
            if (String.IsNullOrEmpty(data)) continue;
            //Debug.Log("Read data: " + data);
            PupilData pupildata = JsonConvert.DeserializeObject<PupilData>(data);
            if (pupildata.confidence >= 0.60) 
            { 
                if(pupildata.topic.EndsWith(".0."))
                {
                    if(dataSize0 < sizeGoal)
                    {
                        errorx0[dataSize0] = cursor.transform.position.x - basex;
                        errory0[dataSize0] = cursor.transform.position.y - basey;
                        pupil0Data[dataSize0++] = pupildata;
                    //////////////////////////////////////////////////////////////////
                    } else if (dataSize1 == 0)
                    {
                        Array.Copy(pupil0Data, pupil1Data, dataSize0);
                        Array.Copy(errorx0, errorx1, dataSize0);
                        Array.Copy(errory0, errory1, dataSize0);
                        dataSize1 = dataSize0; 
                    }
                    //////////////////////////////////////////////////////////////////
                }
                else
                {
                    if (dataSize1 < sizeGoal)
                    {
                        errorx1[dataSize1] = cursor.transform.position.x - basex;
                        errory1[dataSize1] = cursor.transform.position.y - basey;
                        pupil1Data[dataSize1++] = pupildata;
                    //////////////////////////////////////////////////////////////////
                    }
                    else if (dataSize0 == 0)
                    {
                        Array.Copy(pupil1Data, pupil0Data, dataSize1);
                        Array.Copy(errorx1, errorx0, dataSize1);
                        Array.Copy(errory1, errory0, dataSize1);
                        dataSize0 = dataSize1; 
                    }
                    //////////////////////////////////////////////////////////////////
                }
            }
        }

        int dataSize = dataSize0 + dataSize1;
        float ratio = (float)dataSize / ((float)sizeGoal * 2f);
        Color newColor = new Color(1f - ratio, ratio / 3f, 0F);
        Material m = point.GetComponent<Renderer>().material;
        m.SetColor("_EmissionColor", newColor);
        Debug.Log("Scene: "+clickCounter+" | Size 0: "+dataSize0+" | Size 1: " +dataSize1);
    }

    void FinishCalibrationAtScene(int scene)
    {
        GameObject point;
        switch (scene)
        {
            case 1: point = point1; break;
            case 2: point = point2; break;
            case 3: point = point3; break;
            case 4: point = point4; break;
            default: point = point1; break;
        }
        float t0x = 0, t0y = 0, t1x = 0, t1y = 0, e0x = 0, e1x = 0, e0y = 0, e1y = 0;

        // add up last 80 data points
        for (int i = skipSize; i < sizeGoal; i++)
        {
            t0x += pupil0Data[i].norm_pos[0];
            t0y += pupil0Data[i].norm_pos[1];
            t1x += 1 - pupil1Data[i].norm_pos[0];
            t1y += 1 - pupil1Data[i].norm_pos[1];
            e0x += errorx0[i];
            e1x += errorx1[i];
            e0y += errory0[i];
            e1y += errory1[i];
        }

        // take the average
        e0x /= (float)(sizeGoal - skipSize);
        e1x /= (float)(sizeGoal - skipSize);
        e0y /= (float)(sizeGoal - skipSize);
        e1y /= (float)(sizeGoal - skipSize);
        o0x[scene] = t0x / (float)(sizeGoal - skipSize) - 0.5f;
        o0y[scene] = t0y / (float)(sizeGoal - skipSize) - 0.5f;
        o1x[scene] = t1x / (float)(sizeGoal - skipSize) - 0.5f;
        o1y[scene] = t1y / (float)(sizeGoal - skipSize) - 0.5f;

        // normalized position --> relative position
        o0x[scene] *= boardWidth;
        o0y[scene] *= boardHeight;
        o1x[scene] *= boardWidth;
        o1y[scene] *= boardHeight;

        // add the error 
        o0x[scene] += e0x;
        o0y[scene] += e0y;
        o1x[scene] += e1x;
        o1y[scene] += e1y;

        if (scene == 4)
        {

            //  THIS MAY BE THE PROBLEM BECAUSE THIS SECTION IS CALCULATED USING P0 BUT IS USED FOR P1

            // RIGHT 0
            // LEFT 1
            // r = points on calibration plane
            // o = corrected average position of pupil 

            GlobalVars.k01 = ((rx[1] - rx[2]) * (o0y[2] - o0y[3]) - (rx[2] - rx[3]) * (o0y[1] - o0y[2])) /
                ((o0x[1] - o0x[2]) * (o0y[2] - o0y[3]) - (o0x[2] - o0x[3]) * (o0y[1] - o0y[2]));
            GlobalVars.k02 = ((rx[1] - rx[2]) * (o0x[2] - o0x[3]) - (rx[2] - rx[3]) * (o0x[1] - o0x[2])) /
                ((o0y[1] - o0y[2]) * (o0x[2] - o0x[3]) - (o0y[2] - o0y[3]) * (o0x[1] - o0x[2]));
            GlobalVars.k03 = rx[1] - GlobalVars.k01 * o0x[1] - GlobalVars.k02 * o0y[1];
            GlobalVars.k04 = ((ry[1] - ry[2]) * (o0y[2] - o0y[3]) - (ry[2] - ry[3]) * (o0y[1] - o0y[2])) /
                ((o0x[1] - o0x[2]) * (o0y[2] - o0y[3]) - (o0x[2] - o0x[3]) * (o0y[1] - o0y[2]));
            GlobalVars.k05 = ((ry[1] - ry[2]) * (o0x[2] - o0x[3]) - (ry[2] - ry[3]) * (o0x[1] - o0x[2])) /
                ((o0y[1] - o0y[2]) * (o0x[2] - o0x[3]) - (o0y[2] - o0y[3]) * (o0x[1] - o0x[2]));
            GlobalVars.k06 = ry[4] - GlobalVars.k04 * o0x[4] - GlobalVars.k05 * o0y[4];

            GlobalVars.k11 = ((rx[1] - rx[2]) * (o1y[2] - o1y[3]) - (rx[2] - rx[3]) * (o1y[1] - o1y[2])) /
                ((o1x[1] - o1x[2]) * (o1y[2] - o1y[3]) - (o1x[2] - o1x[3]) * (o1y[1] - o1y[2]));
            GlobalVars.k12 = ((rx[1] - rx[2]) * (o1x[2] - o1x[3]) - (rx[2] - rx[3]) * (o1x[1] - o1x[2])) /
                ((o1y[1] - o1y[2]) * (o1x[2] - o1x[3]) - (o1y[2] - o1y[3]) * (o1x[1] - o1x[2]));
            GlobalVars.k13 = rx[1] - GlobalVars.k11 * o1x[1] - GlobalVars.k12 * o1y[1];
            GlobalVars.k14 = ((ry[1] - ry[2]) * (o1y[2] - o1y[3]) - (ry[2] - ry[3]) * (o1y[1] - o1y[2])) /
                ((o1x[1] - o1x[2]) * (o1y[2] - o1y[3]) - (o1x[2] - o1x[3]) * (o1y[1] - o1y[2]));
            GlobalVars.k15 = ((ry[1] - ry[2]) * (o1x[2] - o1x[3]) - (ry[2] - ry[3]) * (o1x[1] - o1x[2])) /
                ((o1y[1] - o1y[2]) * (o1x[2] - o1x[3]) - (o1y[2] - o1y[3]) * (o1x[1] - o1x[2]));
            GlobalVars.k16 = ry[4] - GlobalVars.k14 * o1x[4] - GlobalVars.k15 * o1y[4];

        } 

        Color newColor = new Color(0F, 1F, 0F);
        Material m = point.GetComponent<Renderer>().material;
        m.SetColor("_EmissionColor", newColor);
        dataSize0 = 0; dataSize1 = 0;
        clickable = true;
        if (clickCounter == 4)
        {
            GameObject textObject = GameObject.Find("Description");
            textObject.GetComponent<TextMesh>().text = "Click again to exit.";
        }
        else
        {
            GameObject textObject = GameObject.Find("Description");
            textObject.GetComponent<TextMesh>().text = "Click to start next calibration.";
        }
    }
    // Update is called once per frame
    void Update () {
        if (!clickable)
        {
            if (dataSize0 < sizeGoal || dataSize1 < sizeGoal)
            {
                CollectCalibrationDataAtScene(clickCounter);
            } else
            {
                FinishCalibrationAtScene(clickCounter);
            }
        }
	}
}
