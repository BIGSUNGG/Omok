using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Network;
using System;

public class UI_OmokBoard : MonoBehaviour
{
    [SerializeField]
    GameObject _gameWin;
    [SerializeField]
    GameObject _gameLose;

    [SerializeField]
    GameObject _omokPostionPrefab;
    [SerializeField]
    GameObject _positionGrid;
    [SerializeField]
    GameObject _myStoneText;
    [SerializeField]
    GameObject _curTurnText;

    List<List<UI_OmokPosition>> _positions;

    OmokController _omok { get; set; }

    protected virtual void Start()
    {
        _omok = GameObject.Find("OmokController").GetComponent<OmokController>();
        if (_omok == null)
            Debug.LogWarning("OmokController is null");

        // 오목판 설정
        _positions = new List<List<UI_OmokPosition>>(15);
        for (int x = 0; x < 15; x++)
        {
            int localX = x;

            var positionsRow = new List<UI_OmokPosition>(15);
            _positions.Add(positionsRow);

            for (int y = 0; y < 15; y++)
            {
                int localY = y;

                GameObject omokPositionGo = Instantiate(_omokPostionPrefab, _positionGrid.transform);
                UI_OmokPosition omokPosition = omokPositionGo.GetComponent<UI_OmokPosition>();
                omokPosition.OnClick.AddListener(() => OnClickPosition(localX, localY));
                positionsRow.Add(omokPosition);
            }
        }
    }

    protected virtual void Update()
    {
        if (_omok)
        {
            _myStoneText.GetComponent<Text>().text = $"나의 돌 : {(_omok.MyStone == StoneType.Black ? "흑돌" : "백돌")}";
            _curTurnText.GetComponent<Text>().text = $"현재 턴 : {(_omok.CurTurn == StoneType.Black ? "흑돌" : "백돌")}";
        }
    }

    /// <summary>
    /// 수가 두어졌을 경우 호출
    /// </summary>
    /// <param name="type"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void OnPlace(StoneType type, int x, int y)
    {
        UI_OmokPosition position = _positions[x][y];
        if (position == null)
            return;

        position.OnPlaceStone(type);
    }

    /// <summary>
    /// 오목판의 위치 클릭 시 호출
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    protected void OnClickPosition(int x, int y)
    {
        _omok.Place(x, y);
    }

    /// <summary>
    /// 게임 종료 시 호출
    /// </summary>
    /// <param name="winner">이긴 돌</param>
    public void OnFinishGame(StoneType winner)
    {
        if (_omok.MyStone == winner)
            Instantiate(_gameWin, transform);
        else
            Instantiate(_gameLose, transform);
    }
}
