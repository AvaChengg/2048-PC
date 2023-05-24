using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public enum GameState
{
    GenerateLevel,
    SpawningBlocks,
    WaitingInput,
    Moving,
    Win,
    Lose
}

public class GameController : MonoBehaviour
{
    [Header("Grid Setting")]
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;

    [Header("Square Setting")]
    [SerializeField] private float _movingTime = 0.2f;
    [SerializeField] private Node _node;
    [SerializeField] private Block _block;
    [SerializeField] private SpriteRenderer _board;
    [SerializeField] private List<BlockType> _types;

    [Header("Win or Lose")]
    [SerializeField] private int _winCondition = 2048;

    // private
    private List<Node> _nodes;
    private List<Block> _blocks;
    private GameState _state;
    private int _round;
    private AudioSource _audioSource;

    private BlockType GetBlockTypeByNumber(int number) => _types.First(t => t.Number == number);

    public UnityEvent OnWin;
    public UnityEvent OnLose;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        ChangeState(GameState.GenerateLevel);
    }

    private void ChangeState(GameState newState)
    {
        _state = newState;

        switch (newState)
        {
            case GameState.GenerateLevel:
                GenerateGrid();
                break;
            case GameState.SpawningBlocks:
                SpawnBlocks(_round++ == 0 ? 2 : 1);
                break;
            case GameState.WaitingInput:
                break;
            case GameState.Moving:
                break;
            case GameState.Win:
                OnWin.Invoke();
                break;
            case GameState.Lose:
                OnLose.Invoke();
                break;
        }
    }

    private void Update()
    {
        if (_state != GameState.WaitingInput) return;

        if (Input.GetKeyDown(KeyCode.A)) ShiftDirection(Vector2.left);
        if (Input.GetKeyDown(KeyCode.D)) ShiftDirection(Vector2.right);
        if (Input.GetKeyDown(KeyCode.W)) ShiftDirection(Vector2.up);
        if (Input.GetKeyDown(KeyCode.S)) ShiftDirection(Vector2.down);
    }

    private void GenerateGrid()
    {
        _nodes = new List<Node>();
        _blocks = new List<Block>();

        // make a grid, set x and y position
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                // spawn nodes
                Node node = Instantiate(_node, new Vector2(x, y), Quaternion.identity);
                _nodes.Add(node);
            }
        }

        // set the grid in the middle of the screen
        Vector2 center = new Vector2((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f);

        // spawn grid
        SpriteRenderer board = Instantiate(_board, center, Quaternion.identity);
        board.size = new Vector2(_width, _height);

        Camera.main.transform.position = new Vector3(center.x, center.y, -10f);

        ChangeState(GameState.SpawningBlocks);
    }

    private void SpawnBlock(Node node, int number)
    {
        Block block = Instantiate(_block, node.Position, Quaternion.identity);
        block.Init(GetBlockTypeByNumber(number));
        block.SetBlock(node);
        _blocks.Add(block);
    }

    private void SpawnBlocks(int amount)
    {
        List<Node> emptyNodes = _nodes.Where(n => n.OccupieBlock == null).OrderBy(b => UnityEngine.Random.value).ToList();

        foreach (var node in emptyNodes.Take(amount)) SpawnBlock(node, UnityEngine.Random.value > 0.8f ? 4 : 2);

        // lost the game
        if (emptyNodes.Count() == 1)
        {
            ChangeState(GameState.Lose);
            return;
        }

        ChangeState(_blocks.Any(b => b.Number == _winCondition) ? GameState.Win : GameState.WaitingInput);

    }

    private void ShiftDirection(Vector2 direction)
    {
        ChangeState(GameState.Moving);

        // move all the nodes to the left
        var orderedBlocks = _blocks.OrderBy(b => b.Position.x).ThenBy(b => b.Position.y);
        if (direction == Vector2.right || direction == Vector2.up) orderedBlocks.Reverse();

        foreach (var block in orderedBlocks)
        {
            var next = block.Node;
            do
            {
                block.SetBlock(next);
                var possibleNode = GetNodeAtPosition(next.Position + direction);

                if (possibleNode != null)
                {
                    if (possibleNode.OccupieBlock != null && possibleNode.OccupieBlock.CanMerge(block.Number))
                    {
                        block.MergeBlock(possibleNode.OccupieBlock);
                    }
                    else if (possibleNode.OccupieBlock == null) next = possibleNode;
                }
            }
            while (next != block.Node);
        }

        var sequence = DOTween.Sequence();

        foreach (var block in orderedBlocks)
        {
            var movePoint = block.MergingBlock != null ? block.MergingBlock.Node.Position : block.Node.Position;
            sequence.Insert(0, block.transform.DOMove(movePoint, _movingTime));
        }

        foreach (var block in orderedBlocks.Where(b => b.MergingBlock != null)) MergeBlocks(block.MergingBlock, block);

        ChangeState(GameState.SpawningBlocks);
    }

    private Node GetNodeAtPosition(Vector2 position)
    {
        return _nodes.FirstOrDefault(n => n.Position == position);
    }

    private void MergeBlocks(Block baseBlock, Block mergingBlock)
    {
        _audioSource.Play();
        SpawnBlock(baseBlock.Node, baseBlock.Number * 2);

        RemoveBlock(baseBlock);
        RemoveBlock(mergingBlock);
    }

    private void RemoveBlock(Block block)
    {
        _blocks.Remove(block);
        Destroy(block.gameObject);
    }
}
