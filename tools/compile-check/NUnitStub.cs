// Stub NUnit minimal : sert UNIQUEMENT à vérifier que les tests compilent
// (Unity compile les tests avec le reste : une erreur ici bloque tout le projet).
using System;
using System.Collections;

namespace NUnit.Framework
{
    public class TestAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public TestCaseAttribute(params object[] arguments) { }
        public string TestName { get; set; }
        public object ExpectedResult { get; set; }
    }
    public class TestFixtureAttribute : Attribute { }
    public class SetUpAttribute : Attribute { }

    public class Constraint
    {
        public Constraint Within(double tolerance) => this;
        public Constraint Within(int tolerance) => this;
        public Constraint Percent => this;
    }

    public class NotConstraint
    {
        public Constraint Null => new Constraint();
        public Constraint Empty => new Constraint();
        public Constraint EqualTo(object expected) => new Constraint();
        public Constraint Contain(object expected) => new Constraint();
    }

    public static class Is
    {
        public static Constraint EqualTo(object expected) => new Constraint();
        public static Constraint GreaterThan(object v) => new Constraint();
        public static Constraint GreaterThanOrEqualTo(object v) => new Constraint();
        public static Constraint LessThan(object v) => new Constraint();
        public static Constraint LessThanOrEqualTo(object v) => new Constraint();
        public static Constraint True => new Constraint();
        public static Constraint False => new Constraint();
        public static Constraint Null => new Constraint();
        public static Constraint Empty => new Constraint();
        public static NotConstraint Not => new NotConstraint();
    }

    public static class Does
    {
        public static Constraint Contain(object expected) => new Constraint();
        public static Constraint StartWith(string expected) => new Constraint();
        public static Constraint EndWith(string expected) => new Constraint();
        public static NotConstraint Not => new NotConstraint();
    }

    public static class Assert
    {
        public static void That(object actual, Constraint constraint) { }
        public static void That(object actual, Constraint constraint, string message) { }
        public static void That(object actual, Constraint constraint, string message, params object[] args) { }
    }
}
