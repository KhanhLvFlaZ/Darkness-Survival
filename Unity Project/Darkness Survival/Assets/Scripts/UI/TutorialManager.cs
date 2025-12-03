using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DarknessSurvival.UI
{
    /// <summary>
    /// Lightweight tutorial driver that can display a sequence of instructional steps
    /// using a simple panel (TextMeshPro text + buttons). Designed to run when a gameplay
    /// scene starts and can be skipped once the player has completed it once.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Serializable]
        public struct TutorialStep
        {
            [TextArea]
            public string message;
            [Tooltip("If true, gameplay continues. If false, tutorial pauses the game for this step.")]
            public bool allowGameplayDuringStep;
            [Tooltip("Optional auto-advance delay in seconds (uses realtime, so it still works while paused). Leave <= 0 to require manual Next press.")]
            public float autoAdvanceDelay;
        }

        [Header("Runtime settings")]
        [SerializeField] bool runAutomaticallyOnStart = true;
        [SerializeField] bool allowReplayInEditor = true;
        [SerializeField] string completionPrefKey = "TutorialCompleted";

        [Header("UI wiring")]
        [SerializeField] GameObject panelRoot;
        [SerializeField] TextMeshProUGUI messageLabel;
        [SerializeField] Button nextButton;
        [SerializeField] Button skipButton;

        [Header("Content")]
        [SerializeField] List<TutorialStep> steps = new List<TutorialStep>();

        [Header("Events")]
        [SerializeField] UnityEvent onTutorialStarted;
        [SerializeField] UnityEvent onTutorialCompleted;

        int currentStepIndex = -1;
        Coroutine autoAdvanceRoutine;
        bool tutorialPausedGame;
        bool tutorialCompleted;

        void Awake()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        void Start()
        {
            if (!runAutomaticallyOnStart)
            {
                return;
            }

#if UNITY_EDITOR
            bool skip = !allowReplayInEditor && PlayerPrefs.GetInt(completionPrefKey, 0) == 1;
#else
            bool skip = PlayerPrefs.GetInt(completionPrefKey, 0) == 1;
#endif
            if (skip)
            {
                tutorialCompleted = true;
                return;
            }

            BeginTutorial();
        }

        /// <summary>
        /// Starts the tutorial sequence manually if it was not configured to auto-run.
        /// </summary>
        public void BeginTutorial()
        {
            if (steps.Count == 0)
            {
                CompleteTutorial();
                return;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            currentStepIndex = -1;
            tutorialCompleted = false;
            onTutorialStarted?.Invoke();
            ShowNextStep();
        }

        public void HandleNextButtonPressed()
        {
            if (tutorialCompleted)
            {
                return;
            }

            ShowNextStep();
        }

        public void HandleSkipButtonPressed()
        {
            CompleteTutorial();
        }

        void ShowNextStep()
        {
            ShowStep(currentStepIndex + 1);
        }

        void ShowStep(int stepIndex)
        {
            if (autoAdvanceRoutine != null)
            {
                StopCoroutine(autoAdvanceRoutine);
                autoAdvanceRoutine = null;
            }

            if (stepIndex >= steps.Count)
            {
                CompleteTutorial();
                return;
            }

            currentStepIndex = stepIndex;
            TutorialStep step = steps[stepIndex];

            if (messageLabel != null)
            {
                messageLabel.text = step.message;
            }

            UpdatePauseState(step.allowGameplayDuringStep);
            UpdateButtons(step);

            if (step.autoAdvanceDelay > 0f)
            {
                autoAdvanceRoutine = StartCoroutine(AutoAdvance(step.autoAdvanceDelay));
            }
        }

        IEnumerator AutoAdvance(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            autoAdvanceRoutine = null;
            ShowNextStep();
        }

        void UpdateButtons(TutorialStep step)
        {
            bool waitingForManualInput = step.autoAdvanceDelay <= 0f;
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(waitingForManualInput);
                nextButton.interactable = waitingForManualInput;
            }

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(true);
                skipButton.interactable = true;
            }
        }

        void UpdatePauseState(bool allowGameplay)
        {
            bool shouldPause = !allowGameplay;
            if (shouldPause)
            {
                RequestPause();
            }
            else
            {
                ReleasePause();
            }
        }

        void RequestPause()
        {
            if (tutorialPausedGame)
            {
                return;
            }

            if (PauseManager.instance != null)
            {
                PauseManager.instance.PauseGame();
            }
            else
            {
                Time.timeScale = 0f;
            }

            tutorialPausedGame = true;
        }

        void ReleasePause()
        {
            if (!tutorialPausedGame)
            {
                return;
            }

            if (PauseManager.instance != null)
            {
                PauseManager.instance.UnPauseGame();
            }
            else
            {
                Time.timeScale = 1f;
            }

            tutorialPausedGame = false;
        }

        void CompleteTutorial()
        {
            if (tutorialCompleted)
            {
                return;
            }

            tutorialCompleted = true;
            ReleasePause();

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            PlayerPrefs.SetInt(completionPrefKey, 1);
            PlayerPrefs.Save();
            onTutorialCompleted?.Invoke();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(HandleSkipButtonPressed);
                skipButton.onClick.AddListener(HandleSkipButtonPressed);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNextButtonPressed);
                nextButton.onClick.AddListener(HandleNextButtonPressed);
            }
        }
#endif
    }
}
