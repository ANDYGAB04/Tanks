using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        public int m_NumRoundsToWin = 5;
        public float m_StartDelay = 3f;
        public float m_EndDelay = 3f;
        public CameraControl m_CameraControl;
        public Text m_MessageText;
        public GameObject m_TankPrefab;
        public TankManager[] m_Tanks;
        public Button m_PauseButton;

        private int m_RoundNumber;
        private WaitForSeconds m_StartWait;
        private WaitForSeconds m_EndWait;
        private TankManager m_RoundWinner;
        private TankManager m_GameWinner;
        private bool m_IsPaused = false;

        private void Start()
        {
            m_StartWait = new WaitForSeconds(m_StartDelay);
            m_EndWait = new WaitForSeconds(m_EndDelay);

            SpawnAllTanks();
            SetCameraTargets();
            InitializePauseButton();

            StartCoroutine(GameLoop());
        }

        private void SpawnAllTanks()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                m_Tanks[i].m_Instance = Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation);
                m_Tanks[i].m_PlayerNumber = i + 1;
                m_Tanks[i].Setup();
            }
        }

        private void SetCameraTargets()
        {
            Transform[] targets = new Transform[m_Tanks.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = m_Tanks[i].m_Instance.transform;
            }
            m_CameraControl.m_Targets = targets;
        }

        private IEnumerator GameLoop()
        {
            yield return StartCoroutine(RoundStarting());
            yield return StartCoroutine(RoundPlaying());
            yield return StartCoroutine(RoundEnding());

            if (m_GameWinner != null)
            {
                SceneManager.LoadScene(0);
            }
            else
            {
                StartCoroutine(GameLoop());
            }
        }

        private IEnumerator RoundStarting()
        {
            ResetAllTanks();
            DisableTankControl();
            m_CameraControl.SetStartPositionAndSize();

            m_RoundNumber++;
            m_MessageText.text = "ROUND " + m_RoundNumber;

            yield return m_StartWait;
        }

        private IEnumerator RoundPlaying()
        {
            EnableTankControl();
            m_MessageText.text = string.Empty;
            InitializePauseButton();

            float roundTime = 0f;
            bool suddenDeathTriggered = false;

            while (!OneTankLeft())
            {
                roundTime += Time.deltaTime;

                if (roundTime >= 30f && !suddenDeathTriggered)
                {
                    suddenDeathTriggered = true;
                    StartCoroutine(SuddenDeath());
                }

                yield return null;
            }
        }

        private IEnumerator RoundEnding()
        {
            DisableTankControl();
            m_RoundWinner = GetRoundWinner();

            if (m_RoundWinner != null)
                m_RoundWinner.m_Wins++;

            m_GameWinner = GetGameWinner();
            m_MessageText.text = EndMessage();

            yield return m_EndWait;
        }

        private bool OneTankLeft()
        {
            int numTanksLeft = 0;
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }
            return numTanksLeft <= 1;
        }

        private TankManager GetRoundWinner()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Instance.activeSelf)
                    return m_Tanks[i];
            }
            return null;
        }

        private TankManager GetGameWinner()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                    return m_Tanks[i];
            }
            return null;
        }

        private string EndMessage()
        {
            string message = "DRAW!";
            if (m_RoundWinner != null)
                message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

            message += "\n\n\n\n";

            for (int i = 0; i < m_Tanks.Length; i++)
            {
                message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Wins + " WINS\n";
            }

            if (m_GameWinner != null)
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

            return message;
        }

        private void ResetAllTanks()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                m_Tanks[i].Reset();
            }
        }

        private void EnableTankControl()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                m_Tanks[i].EnableControl();
            }
        }

        private void DisableTankControl()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                m_Tanks[i].DisableControl();
            }
        }

        private IEnumerator SuddenDeath()
        {
            m_MessageText.text = "⚠️ SUDDEN DEATH INCOMING! ⚠️";
            yield return new WaitForSeconds(5f);
            m_MessageText.text = "🔥 ONE-HIT KILL MODE ACTIVATED! 🔥";

            foreach (var tank in m_Tanks)
            {
                if (tank.m_Instance.activeSelf)
                {
                    TankHealth health = tank.m_Instance.GetComponent<TankHealth>();
                    if (health != null)
                    {
                        health.TakeDamage(health.m_StartingHealth - 1);
                    }
                }
            }

            yield return new WaitForSeconds(5f);
            m_MessageText.text = "";
        }

        private void InitializePauseButton()
        {
            if (m_PauseButton == null)
            {
              m_PauseButton = GameObject.Find("PauseButton")?.GetComponent<Button>();
            }

            if (m_PauseButton != null)
            {
                
                m_PauseButton.onClick.RemoveAllListeners();
                m_PauseButton.onClick.AddListener(TogglePause);
                m_PauseButton.gameObject.SetActive(true);
            }
        }

        public void TogglePause()
        {
            m_IsPaused = !m_IsPaused; // Toggle pause state
            Time.timeScale = m_IsPaused ? 0 : 1; // Pause or resume time

            // Update the message text
            if (m_MessageText != null)
            {
                m_MessageText.text = m_IsPaused ? "Game Paused" : "Game Resumed";
                StartCoroutine(ClearMessageAfterDelay(1f)); // Start coroutine to clear the message
            }
        }

        private IEnumerator ClearMessageAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay); // Use WaitForSecondsRealtime to work even when paused
            if (!m_IsPaused) // Only clear the message if the game is not paused
            {
                m_MessageText.text = "";
            }
        }
    }
}
