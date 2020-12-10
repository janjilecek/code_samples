using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class DialogueManager : MonoBehaviour
{
    [Header("Control structures")]
    public Queue<Sentence> sentenceQueue;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public GameObject dialogueCanvas;
    public bool wasPlaying = false;
    public bool wasMePlaying = false;

    [Header("Cached vars")]
    private AudioSource _npcAudioSource;
    private AudioSource _playerAudioSource;
    private God _god;
    private int _sentCounter = 1;
    private int _audioSentCounter = 0;

    void Start()
    {
        dialogueCanvas.SetActive(false);
        //Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;
        _god = GameObject.FindGameObjectWithTag("God").GetComponent<God>();
        sentenceQueue = new Queue<Sentence>();
    }

    public void StartDialogue(Dialogue dialogue)
    {
        Debug.Log("START DIALOGUE");

        dialogueCanvas.SetActive(true);
        _npcAudioSource = _god.collidingNPC.GetComponent<AudioSource>();
        _playerAudioSource = GameObject.FindGameObjectWithTag("Player").GetComponent<AudioSource>();
        Debug.Log("Starting convo " + dialogue.name);
        nameText.text = dialogue.name;

        foreach (Sentence sentence in dialogue.sentences)
        {
            sentenceQueue.Enqueue(sentence);
        }


        //DisplayNextSentence();
        _god.nextConvoSentence();
        try
        {
            _god.kazeta.Stop(); // stopping audiologs
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

    }


    public void showAudiologText(string[] text)
    {
        Debug.LogWarning("starting showAudiologText and starting new routine, stopping previous ones");
        StopAllCoroutines();
        dialogueText.text = text[0];
        _audioSentCounter = 0;
        dialogueCanvas.SetActive(true);
        _god.counterRoutine =  StartCoroutine(counter(text));
    }

    private void showNewSentence(string[] text)
    {
        _audioSentCounter++;

        if (_audioSentCounter > text.Length - 1)
        {
            _audioSentCounter = -1;
            hideAudiologText();
        }
        else
        {
            dialogueText.text = text[_audioSentCounter];
        }

    }

    IEnumerator counter(string[] text)
    {
        if (_audioSentCounter > -1 && _audioSentCounter < text.Length)
        {
            string[] words =  text[_audioSentCounter].Split(' ');


            yield return new WaitForSeconds(words.Length/1.8f); // second per word

            if (words.Length > 0) // guard against alderson loop
            {
                showNewSentence(text);
                Debug.Log(words.Length/1.8f + " I am cotinuing loop");
                _god.counterRoutine =  StartCoroutine(counter(text));
            }
            else
            {
                Debug.Log("words len je 0, kaslu na loop");
            }
        }
    }

    public void hideAudiologText()
    {
        dialogueText.text = "";
        dialogueCanvas.SetActive(false);
        StopAllCoroutines();
    }

    IEnumerator TypeSentence (string sentence)
    {
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return null;
        }
    }

    public void DisplayNextSentence()
    {
        Debug.Log("Display next sentence");
        Debug.LogError("Current millis " + Time.time * 1000);
        if (sentenceQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        Sentence sentenceObj = sentenceQueue.Dequeue();
        String s = sentenceObj.sentence;
        
        if (_sentCounter % 2)
        {

            _playerAudioSource.Stop();
            _npcAudioSource.clip = sentenceObj.sentenceSound;
            _npcAudioSource.Play();
        }
        else
        {

            _npcAudioSource.Stop();
            _playerAudioSource.clip = sentenceObj.sentenceSound;
            _playerAudioSource.Play();
        }


        string sentence = sentenceObj.sentence;
        _sentCounter++; // for audio source switching above
        //StartCoroutine}TypeSentence sentence
        dialogueText.text = sentence;
        Debug.Log(sentence);
    }

    public void EndDialogue()
    {
        Debug.Log("ENd Dialogue");
        try
        {
            _god.collidingNPC.GetComponent<ConvoStarter>().setConvoEnded();
            _sentCounter = 1;
        } catch (Exception e)
        {
            ConvoStarter cs = GameObject.FindObjectOfType<ConvoStarter>();
            Debug.Log("in exception, colliding NPC didnt find, looking everywhere for COnvoStarter");
            if (cs)
            {
                Debug.LogWarning("Convo starter wasnt null, found one, setCOnvoEnd on that");
                cs.setConvoEnded();
            }

            Debug.LogWarning(e);
        }


        dialogueCanvas.SetActive(false);
        Debug.Log("end of convo");
    }


    IEnumerator sentenceSpacer()
    {
        yield return new WaitForSeconds(0.4f);
        DisplayNextSentence();
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            if (!_npcAudioSource.isPlaying && wasPlaying && !_playerAudioSource.isPlaying) // conv ended
            {
                StartCoroutine(sentenceSpacer());
                //DisplayNextSentence(); //
            }

            if (!_playerAudioSource.isPlaying && wasMePlaying && !_npcAudioSource.isPlaying) // conv ended
            {
                StartCoroutine(sentenceSpacer());
                // DisplayNextSentence(); //
            }

            wasMePlaying = _playerAudioSource.isPlaying;
            wasPlaying = _npcAudioSource.isPlaying;
        } catch (Exception e)
        {
            //Debug.Log("pass the exception");
            //pass
        }
    }
}
