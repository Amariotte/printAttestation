namespace ask.Dtos.Reponses
{
    /// <summary>
    /// Classe pour encapsuler les résultats des opérations du repository
    /// </summary>
    /// <typeparam name="T">Type de données retourné</typeparam>
    public class ErreurRepos<T>
    {
        /// <summary>
        /// Indique si l'opération a réussi
        /// </summary>
        public bool success { get; set; }

        /// <summary>
        /// Description du résultat de l'opération
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// Données retournées par l'opération
        /// </summary>
        public T? data { get; set; }
    }
}
