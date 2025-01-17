﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using Constants;
using UnityEngine.SocialPlatforms;

public class QuestionManager : MonoBehaviour
{
	[Header("Question")]
	[SerializeField] private TextMeshProUGUI _questionText;

	[Header("Option Text")]
	[SerializeField] private TextMeshProUGUI _optionText1;
	[SerializeField] private TextMeshProUGUI _optionText2;
	[SerializeField] private TextMeshProUGUI _optionText3;
	[SerializeField] private TextMeshProUGUI _optionText4;

	[Header("Option Button")]
	[SerializeField] private Button _optionButton1;
	[SerializeField] private Button _optionButton2;
	[SerializeField] private Button _optionButton3;
	[SerializeField] private Button _optionButton4;

	[Header("Button List")]
	[SerializeField] private List<OptionButton> _optionButtons;

	[Header("Button Array")]
	[SerializeField] private OptionButton[] _optionButtonArray;

	[Header("Button Parent")]
	[SerializeField] private GameObject _buttonParent;

	[Header("Choose Icon")]
	[SerializeField] private Texture2D _choosenOptionIcon;
	[SerializeField] private Texture2D _correctOptionIcon;

	[Header("Timer")]
	[Range(0f, 30f)]
	[SerializeField] private float m_ResponseTimeLimit;

	[SerializeField] private float m_ResponseTimer;
	public float ResponseTimer
	{
		get => m_ResponseTimer;
		set
		{
			if (m_IsGameOver)
			{
				return;
			}

			m_ResponseTimer = value;
			EventManager.Instance.CountdownTimeIndicator?.Invoke(value);
		}
	}

	[SerializeField] private SecondChanceUI m_SecondChanceUI;

	private bool m_IsQuestionAsked;

	private bool m_IsGameOver;

	public bool m_IsFirstTry;

	private void OnEnable()
	{
		Subscribe();
		ResetValues();

		Debug.Log(FirebaseQuestionManager.questionIDs.Count);

		if (FirebaseQuestionManager.questionIDs.Count > 0)
		{
			StartCoroutine(EventManager.Instance.GetQuestion());
		}
	}

	private void OnDisable()
	{
		GeneralControls.ControlQuit(Unsubscribe);
	}

	#region Event Subscribe/Unsubscribe

	private void Subscribe()
	{
		//EventManager.Instance.GameOver += ResetValues;
		EventManager.Instance.GameOverTrigger += GameOver;
		EventManager.Instance.AskQuestion += AskQuestion;
	}

	private void Unsubscribe()
	{
		//EventManager.Instance.GameOver -= ResetValues;
		EventManager.Instance.GameOverTrigger -= GameOver;
		EventManager.Instance.AskQuestion -= AskQuestion;
	}

	#endregion

	private void ResetValues()
	{
		m_IsQuestionAsked = false;
		m_IsGameOver = false;
		ResponseTimer = m_ResponseTimeLimit;
		EventManager.Instance.UpdateGameUI?.Invoke(m_ResponseTimeLimit);
	}

	private void Update()
	{
		if (m_IsQuestionAsked && !m_IsGameOver)
		{
			if (ResponseTimer >= 0)
			{
				ResponseTimer -= Time.deltaTime;
			}
			else
			{
				StartCoroutine(GameOver(GameOverType.TimesUp));
			}
		}
	}

	private void AskQuestion(Question question)
	{
		_questionText.SetText(question.QuestionText);

		Datas.S_CurrentQuestionLevel = question.QuestionLevel;

		m_IsQuestionAsked = true;

		for (int i = 0; i < _optionButtons.Count; i++)
		{
			int randomOptionButtonIndex = Random.Range(0, question.OptionList.Count);

			_optionButtons[i].Init(question.OptionList[randomOptionButtonIndex], ControlAnswer);

			question.OptionList.RemoveAt(randomOptionButtonIndex);
		}

		foreach (OptionButton optionButton in _optionButtonArray)
		{
			optionButton._optionButton.interactable = true;
		}

		EventManager.Instance.QuestionFadeInAnim?.Invoke();
	}

	private void ControlAnswer(bool answer)
	{
		m_IsQuestionAsked = false;

		foreach (OptionButton optionButton in _optionButtonArray)
		{
			optionButton._optionButton.interactable = false;
		}

		if (answer)
		{
			StartCoroutine(AnsweredCorrectly());
		}
		else
		{
			OptionButton._showCorrectOption?.Invoke();
			AskForSecondChance();
		}
	}

	private IEnumerator AnsweredCorrectly()
	{
		Datas.S_CorrectAnswerAmount++;
		Datas.S_Score += 3 * Datas.S_CurrentQuestionLevel;
		Datas.S_QuestionNumber++;

		yield return new WaitForSeconds(1f);

		ResponseTimer = m_ResponseTimeLimit;

		EventManager.Instance.UpdateGameUI?.Invoke(m_ResponseTimeLimit);

		EventManager.Instance.QuestionFadeOutAnim?.Invoke();
	}

	private void AskForSecondChance()
	{
		m_SecondChanceUI.SetActiveSelf();
	}

	private IEnumerator GameOver(GameOverType gameOverType)
	{
		if (gameOverType == GameOverType.TimesUp)
		{
			Debug.Log("Süre Bitti!");
		}
		else if (gameOverType == GameOverType.WrongAnswer)
		{
			Datas.S_WrongAnswerAmount++;
			Debug.Log("Yanlış Cevap!");
		}

		//EventManager.Instance.GainExperience(_correctAnswerAmount, _wrongAnswerAmount);
		m_IsGameOver = true;

		EventManager.Instance.UpdateUserData(UserPaths.PrimaryPaths.Progression, UserPaths.ProgressionPaths.CorrectAnswers, CurrentUserProfileKeeper.CorrectAnswers + Datas.S_CorrectAnswerAmount);
		EventManager.Instance.UpdateUserData(UserPaths.PrimaryPaths.Progression, UserPaths.ProgressionPaths.WrongAnswers, CurrentUserProfileKeeper.WrongAnswers + Datas.S_WrongAnswerAmount);

		Datas.S_GainedExperience = Datas.S_Score;

		yield return new WaitForSeconds(1f);

		EventManager.Instance.GameOver?.Invoke();
		Datas.ResetValues();
	}
}
