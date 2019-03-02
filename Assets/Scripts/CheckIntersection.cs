// when the gaze and word collider are intersecting for 20 updates
// play the word's audio source when counter if over 20
// note that all the game objects are 'connected' in the unity engine  


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Academy {
    public class CheckIntersection : MonoBehaviour {
        int counter;
        int threshold = 20;
        [SerializeField]
        private GameObject gazePoint;

        private Transform preWord;
         
        // Use this for initialization
        void Start() {
            counter = 0;
        } 

        // Update is called once per frame
        void Update() {
            if (gazePoint != null && gazePoint.activeSelf)
            {
                Collider gazeCollider = gazePoint.GetComponent<Collider>();
                Collider wordCollider = null;
                if (counter < threshold)
                {
                    // checks each word for each update 
                    foreach(Transform word in transform)
                    {
                       if(word != null && word.name.StartsWith("Words-"))
                        {
                            wordCollider = word.GetComponent<Collider>();
                            if (gazeCollider.bounds.Intersects(wordCollider.bounds))
                            {
                                if(preWord != null && string.Equals(preWord.name, word.name))
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

                    preWord.GetComponent<Interactible>().PlaySound();
                }
            }
        }
    }
}
