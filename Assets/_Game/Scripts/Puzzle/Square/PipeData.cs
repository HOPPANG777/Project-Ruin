using System;

[Flags]
public enum PipeDir
{
    None = 0,
    Top = 1 << 0,
    Right = 1 << 1,
    Bottom = 1 << 2,
    Left = 1 << 3
}
public enum PipeType
{
    Empty, 
    Start, 
    End, 
    Pipe_1_Straight, 
    Pipe_1_Corner, 
    Pipe_2_T, 
    Pipe_3_Cross
}

[Serializable]
public struct PipeData
{
    public PipeType type;
    public int rotationIndex;

    public PipeDir GetCurrentDirections()
    {
        PipeDir baseDir = type switch
        {
            PipeType.Start => PipeDir.Top,
            PipeType.End => PipeDir.Top,
            PipeType.Pipe_1_Straight => PipeDir.Top | PipeDir.Bottom,
            PipeType.Pipe_1_Corner => PipeDir.Left | PipeDir.Bottom,
            // T 프리팹의 기본 그림은 왼쪽, 오른쪽, 아래쪽이 열려 있다.
            // 논리 방향도 그림과 같아야 보이지 않는 반대편으로 물이 흐르지 않는다.
            PipeType.Pipe_2_T => PipeDir.Left | PipeDir.Right | PipeDir.Bottom,
            PipeType.Pipe_3_Cross => PipeDir.Top | PipeDir.Bottom | PipeDir.Left | PipeDir.Right,
            _ => PipeDir.None
        };

        int dirVal = (int)baseDir;
        for (int i = 0; i < rotationIndex; i++)
        {
            dirVal = ((dirVal << 1) | (dirVal >> 3)) & 0x0F;
        }

        return (PipeDir)dirVal;
    }
}
