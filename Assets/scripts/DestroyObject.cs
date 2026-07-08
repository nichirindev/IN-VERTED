// Decompiled with JetBrains decompiler
// Type: DestroyObject
// Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: E7BE5432-1537-4547-9AF4-A899CFBF2707
// Assembly location: D:\Games\Win64_3\RERUN_Data\Managed\Assembly-CSharp.dll

using UnityEngine;

#nullable disable
public class DestroyObject : MonoBehaviour
{
  public float time = 2f;

  private void Start() => this.Invoke("DestroySelf", this.time);

  private void DestroySelf() => Object.Destroy((Object) this.gameObject);
}
