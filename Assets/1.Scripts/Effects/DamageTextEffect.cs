using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;


public class DamageTextEffect : MonoBehaviour
{
    private Vector3 originalPosition;
    [SerializeField] TextMeshProUGUI textMeshPro;

    CinemachineCamera mainCamera;

    private void Start()
    {
        originalPosition = transform.position;

        SceneManager.sceneLoaded += FindCamera;

        mainCamera = GameObject.FindWithTag("SceneCamera").GetComponent<CinemachineCamera>();
    }

    void FindCamera(Scene scene, LoadSceneMode mode)
    {
         mainCamera = GameObject.FindWithTag("SceneCamera").GetComponent<CinemachineCamera>();
    }

    //货肺款 纠 积己矫

    public void SetDamage(float damage)
    {
        textMeshPro.text = damage.ToString();

        transform.position += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
        transform.DOMoveY(transform.position.y + 1f, 1f).OnComplete(() => 
        { 
            transform.position = originalPosition;
            gameObject.SetActive(false);
        });
    }

    private void LateUpdate()
    {
        transform.forward = mainCamera.transform.forward;
    }
}
