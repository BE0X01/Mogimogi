using Mogimogi.Core.Models;

namespace Mogimogi.Core.Abstractions;

public interface IQuestionBookReader
{
    QuestionBook Read(string filePath);
}
