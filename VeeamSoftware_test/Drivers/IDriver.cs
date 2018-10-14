using System;
using System.Collections.Generic;

namespace GZipTest.Drivers
{
    public interface IDriver
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