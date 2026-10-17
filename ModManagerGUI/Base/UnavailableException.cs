namespace ModManagerGUI.Base;

public class UnavailableException:ApplicationException {
    public UnavailableException() { }
    public UnavailableException(string message) : base(message) { }
}