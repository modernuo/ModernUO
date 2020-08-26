namespace Server.Misc
{
  public static class Animations
  {
    public static void Initialize()
    {
      EventSink.AnimateRequest += EventSink_AnimateRequest;
    }

    private static void EventSink_AnimateRequest(Mobile from, string actionName)
    {
      int action = actionName switch
      {
        "bow" => 32,
        "salute" => 33,
        _ => 0,
      };

      if (action > 0 && from.Alive && !from.Mounted && from.Body.IsHuman)
        from.Animate(action, 5, 1, true, false, 0);
    }
  }
}
