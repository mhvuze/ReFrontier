using System.IO;

namespace ReFrontier.jpk
{
  public delegate void ShowProgress(long perc);
  interface IJPKEncode
  {
    void WriteByte(Stream s, byte b);
    void ProcessOnEncode(byte[] inBuffer, Stream outStream, int level=16,  ShowProgress progress = null);
  }
}
