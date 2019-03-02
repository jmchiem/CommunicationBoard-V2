using UnityEngine;
using System.IO;
using System.Diagnostics;
using System; 

public class DataCollector : MonoBehaviour
{

    private static bool audioPlayed;
    private static string wordName;
    private static Vector3 gazev;
    private static Vector3 wordv;
    private Vector3 previous;
    private Stopwatch stopwatch;
	private double time;
    private double pretime;

    public FileStream fs;
    public StreamWriter s;
    public FileStream fs2;
    public StreamWriter s2;

    /////////////////////////////////////////////////
    /////////////////////////////////////////////////
    private int counter;
    private int threshold = 20;
    private Transform preWord;

    [SerializeField]
    private GameObject gazePoint;
    /////////////////////////////////////////////////
    /////////////////////////////////////////////////

    void Start()
    {
        stopwatch = new Stopwatch();
        stopwatch.Start();
        audioPlayed = false;
        previous = new Vector3(0, 0, 0);
        pretime = 0;
        // this means the first word will not have accurate data
        // unless users are told to look at 'hello' / the center before starting the sentence 

        string daytime = DateTime.Today.ToString("ddMMyyyy") + "_" + DateTime.Now.ToString("HHmm");
        string path = Application.persistentDataPath + "\\" + daytime + ".csv";
        string path2 = Application.persistentDataPath + "\\" + daytime + "_track" + ".csv";
        // Windows.Storage.KnownFolders.DocumentsLibrary

        try
        {
            fs = new FileStream(path, FileMode.Create);
            s = new StreamWriter(fs);

            fs2 = new FileStream(path2, FileMode.Create);
            s2 = new StreamWriter(fs2);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Error creating file. Exception: " + e.ToString());
        }

        // write columns names to file
        WriteData("word", "wordx", "wordz", "gazex", "gazez", "offset", "distance", "duration");
        WriteGaze("time", "x", "z");

    }

    void LateUpdate()
    {
		
        if (audioPlayed)
        {
            audioPlayed = false; 

            string duration = (time - pretime).ToString();
            pretime = time;

            // assuming that the position of an object gives the center position
            string wordx = wordv.x.ToString();
            string wordz = wordv.z.ToString();
            string gazex = gazev.x.ToString();
            string gazez = gazev.z.ToString();
            string offset = Vector3.Distance(wordv, gazev).ToString();
            string distance = Vector3.Distance(wordv, previous).ToString();
            previous = wordv;

            WriteData(wordName, wordx, wordz, gazex, gazez, offset, distance, duration);

        }
		
        WriteGaze(time.ToString(), gazev.x.ToString(), gazev.z.ToString());
    }

    //   source, word, word positions, gaze positions, offset, distance, duration
    void WriteData(string w, string wx, string wz, string gx, string gz, string o, string ds, string dr)
    {
        string concat = w+","+wx+","+wz+","+gx+","+gz+","+o+","+ds+","+dr+Environment.NewLine;

        try
        {
            s.Write(concat);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Error writing to file. Exception: " + e.ToString());
        }

    }

    void WriteGaze(string ms, string x, string z)
    {
        string concat = ms + "," + x + "," + z + Environment.NewLine;

        try
        {
            s2.Write(concat); 
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Error writing to file. Exception: " + e.ToString());
        }
    }

    void OnDisable()
    {
        s.Dispose();
        fs.Dispose();
        s2.Dispose();
        fs2.Dispose();
    }

    private void OnDestroy()
    {
        if (s != null) 
            s.Dispose();
        if (fs != null)
            fs.Dispose();
        if (s2 != null)
            s2.Dispose();
        if (fs2 != null)
            fs2.Dispose();
    }

    /////////////////////////////////////////////////
    /////////////////////////////////////////////////

    void Update()
    {
        if (gazePoint != null && gazePoint.activeSelf)
        {
            Collider gazeCollider = gazePoint.GetComponent<Collider>();
            Collider wordCollider = null;
            if (counter < threshold)
            {
                // checks each word for each update 
                foreach (Transform word in transform)
                {
                    if (word != null && word.name.StartsWith("Words-"))
                    {
                        wordCollider = word.GetComponent<Collider>();
                        if (gazeCollider.bounds.Intersects(wordCollider.bounds))
                        {
                            if (preWord != null && string.Equals(preWord.name, word.name))
                            {
                                counter++;
                            }
                            else
                            {
                                counter = 1;
                                preWord = word;
                            }
                            // Debug.Log(word.name + ":" + counter);
                            break;
                        }
                    }
                }
            }
            else
            {
                counter = 0;
				audioPlayed = true;
				wordv = preWord.localPosition;
                wordName = preWord.name.Substring(6); 

            }
			
			time = stopwatch.Elapsed.TotalSeconds;
            gazev = gazePoint.transform.localPosition;
        }
    }

    /////////////////////////////////////////////////
    /////////////////////////////////////////////////

}

