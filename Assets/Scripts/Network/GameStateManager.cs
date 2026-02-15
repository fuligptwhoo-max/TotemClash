using UnityEngine;
using UnityEngine.Events;

namespace TotemClash.Network
{
    /// <summary>
    /// Управляет состоянием игры (Single Player Version)
    /// Заменяет GameStateNetworkSync
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }
        
        [Header("Events")]
        public UnityEvent OnGameStarted;
        public UnityEvent OnGameEnded;
        public UnityEvent OnGamePaused;
        public UnityEvent OnGameResumed;
        
        private bool isGameStarted = false;
        private bool isGamePaused = false;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Вызывается при старте игры
        /// </summary>
        public void StartGame()
        {
            isGameStarted = true;
            isGamePaused = false;
            OnGameStarted?.Invoke();
            Debug.Log("[GameStateManager] Game started");
        }
        
        /// <summary>
        /// Вызывается при окончании игры
        /// </summary>
        public void EndGame()
        {
            isGameStarted = false;
            OnGameEnded?.Invoke();
            Debug.Log("[GameStateManager] Game ended");
        }
        
        /// <summary>
        /// Ставит игру на паузу
        /// </summary>
        public void PauseGame()
        {
            if (!isGamePaused)
            {
                isGamePaused = true;
                Time.timeScale = 0f;
                OnGamePaused?.Invoke();
                Debug.Log("[GameStateManager] Game paused");
            }
        }
        
        /// <summary>
        /// Возобновляет игру
        /// </summary>
        public void ResumeGame()
        {
            if (isGamePaused)
            {
                isGamePaused = false;
                Time.timeScale = 1f;
                OnGameResumed?.Invoke();
                Debug.Log("[GameStateManager] Game resumed");
            }
        }
        
        public bool IsGameStarted()
        {
            return isGameStarted;
        }
        
        public bool IsGamePaused()
        {
            return isGamePaused;
        }
    }
}
