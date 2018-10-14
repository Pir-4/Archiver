using System;
using System.Collections.Generic;

namespace GZipTest.GZipDriver
{
    public interface IGzipDriver
    {
        /// <summary>
        /// Преобразование данных
        /// </summary>
        /// <param name="inputPath">путь до входного файла</param>
        /// <param name="outputPath">путь до выходного файла</param>
        void Execute(string inputPath, string outputPath);

        /// <summary>
        /// Получение список ошибок
        /// </summary>
        List<Exception> Exceptions { get; }
    }
}