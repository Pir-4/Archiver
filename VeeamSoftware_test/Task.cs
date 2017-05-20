using System;

namespace VeeamSoftware_test
{
    /// <summary>
    /// Класс представляет задачу для выполнения в <see cref="FixedThreadPool"/>
    /// </summary>
    public class Task
    {

        private Action work;
        private bool isRunned;

        /// <summary>
        /// Создает задачу с указанным приоритетом.
        /// </summary>
        /// <param name="work">Делегат содержащий метода для задачи.</param>
        public Task(Action work)
        {
            this.work = work;
        }

        /// <summary>
        /// Запускает задачу.
        /// </summary>
        public void Execute()
        {
            lock (this)
            {
                isRunned = true;
            }
            work();
        }

        /// <summary>
        /// Запущена ли задача. (True - запущена, False - стоит в очереди на выполнение)
        /// </summary>
        public bool IsRunned
        {
            get
            {
                return isRunned;
            }
        }

    }
}