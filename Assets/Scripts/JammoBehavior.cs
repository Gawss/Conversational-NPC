using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

using Unity.Sentis;

using Newtonsoft.Json;

/// <summary>
/// This class is used to control the behavior of our Robot (State Machine and Utility function)
/// </summary>
public class JammoBehavior : MonoBehaviour
{
    /// <summary>
    /// The Robot Action List
    /// </summary>
    [System.Serializable]
    public struct Actions
    {
        public string sentence;
        public string verb;
        public string noun;
    }

    /// <summary>
    /// Enum of the different possible states of our Robot
    /// </summary>
    private enum State
    {
        Bye,
        Idle,
        Jump, // Say hello
        Slide
    }

    [Header("Robot Brain")]
    public SentenceSimilarity jammoBrain;

    [Header("Robot list of actions")]
    public List<Actions> actionsList;

    private State state;

    [HideInInspector]
    public List<string> sentences; // Robot list of sentences (actions)
    public string[] sentencesArray;

    [HideInInspector]
    public float maxScore;
    public int maxScoreIndex;


    private void Awake()
    {
        // Set the State to Idle
        state = State.Idle;

        // Take all the possible actions in actionsList
        foreach (JammoBehavior.Actions actions in actionsList)
        {
            sentences.Add(actions.sentence);
        }
        sentencesArray = sentences.ToArray();
    }


    /// <summary>
    /// Utility function: Given the results of HuggingFaceAPI, select the State with the highest score
    /// </summary>
    /// <param name="maxValue">Value of the option with the highest score</param>
    /// <param name="maxIndex">Index of the option with the highest score</param>
    public void Utility(float maxScore, int maxScoreIndex)
    {
        // First we check that the score is > of 0.2, otherwise we let our agent perplexed;
        // This way we can handle strange input text (for instance if we write "Go see the dog!" the agent will be puzzled).
        if (maxScore < 0.20f)
        {
            state = State.Bye;
        }
        else
        {
            // // Get the verb and noun (if there is one)
            // goalObject = GameObject.Find(actionsList[maxScoreIndex].noun);

            string verb = actionsList[maxScoreIndex].verb;

            // Set the Robot State == verb
            state = (State)System.Enum.Parse(typeof(State), verb, true);
        }
    }


    /// <summary>
    /// When the user finished to type the order
    /// </summary>
    /// <param name="prompt"></param>
    public void OnOrderGiven(string prompt)
    {
        Tuple<int, float> tuple_ = jammoBrain.RankSimilarityScores(prompt, sentencesArray);
        Utility(tuple_.Item2, tuple_.Item1);
    }
        

    private void Update()
    {
        Debug.Log(state);
        // // Here's the State Machine, where given its current state, the agent will act accordingly
        // switch(state)
        // {
        //     default:
        //     case State.Idle:
        //         break;

        //     case State.Hello:
        //         agent.SetDestination(playerPosition.position);
        //         if (Vector3.Distance(transform.position, playerPosition.position) < reachedPositionDistance)
        //         {
        //             RotateTo();
        //             anim.SetBool("hello", true);
        //             state = State.Idle;
        //         }
        //         break;

        //     case State.Happy:
        //         agent.SetDestination(playerPosition.position);
        //         if (Vector3.Distance(transform.position, playerPosition.position) < reachedPositionDistance)
        //         {
        //             RotateTo();
        //             anim.SetBool("happy", true);
        //             state = State.Idle;
        //         }
        //         break;

        //     case State.Puzzled:
        //         agent.SetDestination(playerPosition.position);
        //         if (Vector3.Distance(transform.position, playerPosition.position) < reachedPositionDistance)
        //         {
        //             RotateTo();
        //             anim.SetBool("puzzled", true);
        //             state = State.Idle;
        //         }
        //         break;

        //     case State.MoveTo:
        //         agent.SetDestination(goalObject.transform.position);
                
        //         if (Vector3.Distance(transform.position, goalObject.transform.position) < reachedPositionDistance)
        //         {
        //             state = State.Idle;
        //         }
        //         break;

        //     case State.BringObject:
        //         // First move to the object
        //         agent.SetDestination(goalObject.transform.position);
        //         if (Vector3.Distance(transform.position, goalObject.transform.position) < reachedObjectPositionDistance)
        //         {
        //             Grab(goalObject);
        //             state = State.BringObjectToPlayer;
        //         }
        //         break;

        //     case State.BringObjectToPlayer:
        //         agent.SetDestination(playerPosition.transform.position);
        //         if (Vector3.Distance(transform.position, playerPosition.transform.position) < reachedObjectPositionDistance)
        //         {
        //             Drop(goalObject);
        //             state = State.Idle;
        //         }
        //         break;
        // }
    }
}
