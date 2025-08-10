namespace MusicService.Models;

public record MediaEntry(
    string RelativeSourceFilePath,
    string RelativeTargetFilePath,
    string AbsoluteSourceFilePath,
    string AbsoluteTargetFilePath);