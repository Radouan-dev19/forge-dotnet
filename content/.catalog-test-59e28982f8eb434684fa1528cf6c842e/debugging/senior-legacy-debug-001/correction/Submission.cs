using System;

public static class Submission
{
    public static int PageCount(int totalItems, int pageSize)
    {
        if (pageSize < 1)
        {
            throw new ArgumentException("La taille de page doit etre positive.", nameof(pageSize));
        }

        if (totalItems < 0)
        {
            throw new ArgumentException("Le total d'elements ne peut pas etre negatif.", nameof(totalItems));
        }

        return (totalItems + pageSize - 1) / pageSize;
    }
}
