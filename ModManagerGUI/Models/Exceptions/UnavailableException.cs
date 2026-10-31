namespace ModManagerGUI.Models.Exceptions;

public class UnavailableException:ApplicationException {
    public UnavailableException() { }
    public UnavailableException(string message) : base(message) { }
}