using System;
using System.Collections;
using System.Collections.Generic;
using _scripts;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Analytics;
using Random = UnityEngine.Random;

public class FallingPlayer : MonoBehaviour
{
    enum tutorialPhases
    {
        tutorial0,
        tutorial1,
        tutorial2
    }

    public Transform tweenSpawnPos;

    [Header("Cached variables")] 
    private GameObject _mainCamera;
    private float speed = 8f;
    private bool _fadeIn = true;
    private float _t;
    private God _god; // persistent god object
    private bool alive = true;
    private tutorialPhases _tutorialPhase;
    private Coroutine _tutTimer;
    private bool _vulnerable = true;
    private Coroutine _flasher;
    private SkinnedMeshRenderer[] _smrArr;
    private Camera _camera;

    void Start()
    {
        _tutorialPhase = tutorialPhases.tutorial0;
        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        _god = FindObjectOfType<God>();
        _camera = _mainCamera.GetComponent<Camera>();

        if (_god.playerIsInTutorial)
        {
            showTutorial();
        }

        _god.HandleRewardBeers();
        GameObject svarta = GameObject.FindGameObjectWithTag("fall_anim");
        _smrArr = svarta.GetComponentsInChildren<SkinnedMeshRenderer>();
        _god.audioManager.PlayMusic(Sound.Tag.FALL_WIND);
        /*AnalyticsEvent.Custom("BeersInFall", new Dictionary<string, object>
        {
            { "beers", _god.GetBeers() },
        });*/
    }

    void showTutorial()
    {
        _god.tutorial.ChangeText("Pád ovládej nakláněním. Vyhýbej se překážkám. Sbírej mince, které ti zvýší skóre.");
        _god.tutorial.ChangeImageToNext();
        _god.tutorial.ShowImage();
        _god.tutorial.ShowTutorialCanvas();
        _tutTimer = StartCoroutine(showNextTutTimer());
        //AnalyticsEvent.TutorialStep(4);
    }

    IEnumerator hideTutorialAfterDelay()
    {
        yield return new WaitForSeconds(5f);
        _god.tutorial.HideTutorialCanvas();
    }

    // Update is called once per frame
    void Update()
    {
        if (!_god.LevelIsLoading)
        {
            androidControls();
        }

        fadeInAfterStart();

        if (_god.playerIsInTutorial && (_god.getTouch() || _god.getMousePlaceholder()))
        {
            Debug.Log(_tutorialPhase);
            StopCoroutine(_tutTimer);
            showNextTutorial();
        }
    }

    IEnumerator showNextTutTimer()
    {
        yield return new WaitForSeconds(6f);
        showNextTutorial();
    }

    void showNextTutorial()
    {
        _tutTimer = StartCoroutine(showNextTutTimer());
        Debug.Log("subr called");
        if (_tutorialPhase == tutorialPhases.tutorial0)
        {
            _god.tutorial.ChangeText("Vlevo je ukazatel skóre a tvého předchozího rekordu");
            _god.tutorial.ShowImage();
            _god.tutorial.ShowTutorialCanvas();
            StartCoroutine(waitAndWobbleLeft());
            _tutorialPhase = tutorialPhases.tutorial1;
            //AnalyticsEvent.TutorialStep(5);
        }
        else if (_tutorialPhase == tutorialPhases.tutorial1)
        {
            _god.tutorial.ChangeText(
                "Vpravo ukazatel piv a tvoje pozice v globálních žebříčcích nejlepších hráčů.");
            _god.tutorial.ShowImage();
            _god.tutorial.ShowTutorialCanvas();
            StartCoroutine(waitAndWobbleRight());
            _tutorialPhase = tutorialPhases.tutorial2;
            //AnalyticsEvent.TutorialStep(6);
        }
        else if (_tutorialPhase == tutorialPhases.tutorial2)
        {
            _god.tutorial.HideTutorialCanvas();
            _god.playerIsInTutorial = false;
            StopCoroutine(_tutTimer);
            _god.playServices.Achieve(PlayServices.Achievement.tutorial);
            _god.saveSystem.SetTutorialShown();
            //AnalyticsEvent.TutorialStep(7);
        }
    }

    IEnumerator waitAndWobbleLeft()
    {
        yield return new WaitForSeconds(1f);
        _god.wobbleScore();
        yield return new WaitForSeconds(1f);
        _god.wobbleRecord();
    }

    IEnumerator waitAndWobbleRight()
    {
        yield return new WaitForSeconds(1f);
        _god.wobbleBeers();
        yield return new WaitForSeconds(1f);
        _god.wobbleGlobal();
    }


    void fadeInAfterStart()
    {
        if (_fadeIn)
        {
            transform.DOMoveY(tweenSpawnPos.hierarchyCapacity, 3f);

            if (Vector3.Distance(transform.position, tweenSpawnPos.position) < 0.01f) // pokud jsme uz na hrane
            {
                _fadeIn = false;
            }
        }
    }

    public void Die()
    {
        alive = false;
    }

    public bool Alive()
    {
        return alive;
    }

    void androidControls()
    {
        Vector3 dir = Vector3.zero;


        // -- BLOCKS PLAYER ON THE EDGE OF THE SCREEN
        Vector3 pos = transform.position;
        Vector3 left = _camera.ViewportToWorldPoint(new Vector3(0, 0.5f, pos.z - _camera.transform.position.z));
        Vector3 right = _camera.ViewportToWorldPoint(new Vector3(1, 0.5f, pos.z - _camera.transform.position.z));

        float inputX = Input.acceleration.x;

        if (pos.x < left.x && inputX < 0 || pos.x > right.x && inputX > 0)
        {
            dir = Vector3.zero;
        }
        else
        {
            dir.x = inputX * 3f;
        }

        dir *= Time.deltaTime;
        // Move object
        transform.Translate(speed * -dir);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_god.LevelIsLoading)
        {
            if (other.gameObject.CompareTag("branch") && _vulnerable)
            {
                Debug.Log("collide branch");

                other.gameObject.transform.DORotate(
                    new Vector3(Random.Range(-180f, 180f), Random.Range(-180f, 180f), Random.Range(-180f, 180f)),
                    3f, RotateMode.FastBeyond360);

                float direction = transform.position.x < other.transform.position.x ? 1 : -1;

                other.gameObject.GetComponent<Rigidbody>().useGravity = true;
                other.gameObject.GetComponent<Rigidbody>()
                    .AddForce(new Vector3(2 * direction, -15, 0), ForceMode.Impulse);
                other.gameObject.GetComponent<BoxCollider>().enabled = false;

                DamageTaken();
                _god.audioManager.PlayMusic(Sound.Tag.ENEMY_HIT_BRANCH);
                _god.audioManager.PlayMusic(Sound.Tag.SVARTA_HIT);
            }
            else if (other.gameObject.CompareTag("enemy") && _vulnerable)
            {
                Debug.Log("collide enemy");
                _god.audioManager.PlayMusic(Sound.Tag.ENEMY_HIT_CROW);
                _god.audioManager.PlayMusic(Sound.Tag.SVARTA_HIT);
                DamageTaken();
                other.gameObject.transform.GetComponent<Animator>().SetTrigger("Leave"); // play animation for damage
                ParticleSystem ps = other.gameObject.GetComponentInChildren<ParticleSystem>();
                if (ps)
                {
                    ps.Play();
                }
            }
            else if (other.gameObject.CompareTag("coin"))
            {
                Debug.Log("get coin");
                GetCoin();
                _god.audioManager.PlayMusic(Sound.Tag.COLLECTIBLES_COIN);
                Destroy(other.gameObject);
            }
        }
    }

    // flashing during invincibility
    IEnumerator invincibleFlash(float seconds = 1f)
    {
        _vulnerable = false;

        if (_flasher != null)
        {
            StopCoroutine(_flasher);
        }

        _flasher = StartCoroutine(flashTimes(seconds));
        yield return new WaitForSeconds(seconds);
        _vulnerable = true;
        enableSMR();
    }

    IEnumerator flashTimes(float waitTime, float flashes = 10f)
    {
        bool on = true;
        for (int index = 0; index <= flashes; index++)
        {
            on = !on;
            enableSMR(on);
            yield return new WaitForSeconds((waitTime - 0.1f) / flashes); // guard against accidental turn off
        }

        enableSMR();
    }

    void enableSMR(bool on = true)
    {
        for (int i = 0; i < _smrArr.Length; i++)
        {
            _smrArr[i].enabled = on;
        }
    }


    void GetCoin()
    {
        _god.PopScore();
        _mainCamera.transform.DOShakePosition(0.1f);
        _god.playServices.AchievementIncrementCoins(); // todo edit
        //_god.UpdatePlayerRank();    
    }

    void DamageTaken()
    {
        _mainCamera.transform.DOShakePosition(0.2f);
        _god.RemoveBeer();
        _god.Vibrate();
        if (_vulnerable)
        {
            StartCoroutine(invincibleFlash());
        }
    }
}