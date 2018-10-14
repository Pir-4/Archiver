using System;
using System.Collections.Generic;

namespace GZipTest.GZipDriver
{
    public interface IGzipDriver
    {
        /// <summary>
        /// Преобразование данных
        /// </summary>
        void Execute();

        /// <summary>
        /// Получение список ошибок
        /// </summary>
        List<Exception> Exceptions { get; }
    }
}