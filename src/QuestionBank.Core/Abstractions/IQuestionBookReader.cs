using QuestionBank.Core.Models;

namespace QuestionBank.Core.Abstractions;

public interface IQuestionBookReader
{
    QuestionBook Read(string filePath);
}
