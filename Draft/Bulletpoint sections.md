### Introduction

By reference to the Journals, give an overview the subject area, the key points in the subject area and what others have done in the topic area over the last four years. Use this referenced material to justify why the subject area is valid as topic area for a final year project.

- OOP is a popular programming paradigm
- There is a lack of understanding or standardisation of software quality
- Maintaining high quality code is important early on and especially as a project grows
- There are many different tools and approaches that factor into measuring software quality:
  - Checkstyle (IEEE-8802820) [tool]
  - Lint4j (IEEE-8681007) [tool]
  - Documentation (ACM-10.1145_3428029.3428047) [approach]
  - Structure (ACM-10.1145_3428029.3428047) [approach]
  - Code smells (ACM-10.1145_3555228.3555268) [approach]
- There is a demand for high quality code (IEEE-6606742) [approach]
- Others have proposed approaches to measuring software quality but have not been widely adopted (IEEE-6606742)

### Problem Domain

By reference to the Journals identify the key problems that most people have worked on or shown interest in.
Identify a problem / question / approach, highlighting its originality and/or significance, explain how it contributes / develops the topic area.  
Identify recommendations for further work that will form the basis of your project.

- OOP has become an extremely popular programming paradigm (IEEE-6606742)
- There is a lack of standardisation in the way software quality is measured (IEEE-6606742, IEEE-8681007)
- Maintaining high quality code makes is important as poor quality code makes it becomes harder to or too expensive maintain code bases as a project grows (IEEE-8802820, ACM-10.1145_2507288.2507312, ACM-10.1145_3379597.3387457, IEEE-6606742, IEEE-7372958)
- Many of the papers in this area go over how important software quality is and the problems around the topic but only few provide approaches and solutions to solve the issues.
  - This is also backed up by the research conducted by (IEEE-6606742) where they found that there is a demand for high quality code and a surplus of therotical solutions but a lack of real world solutions.
- It is diffucult for developers to decide what methodolgys to adopt due to the number of proposed approaches and the lack of standardisation (IEEE-6606742)

### Methodology

By reference to the Journals, identify the variety approaches and methodologies that have been used / developed in the identified problem domain and state, what you will use as a methodology, test-bed, programming framework, simulator, real device, standards, mathematical model.

<!-- - The methodology I will be using is a combination of the approaches proposed by (ACM-10.1145_3428029.3428047) and (ACM-10.1145_3555228.3555268)
  - (ACM-10.1145_3428029.3428047) proposes a method of measuring software quality by analysing the documentation and structure of a project
  - (ACM-10.1145_3555228.3555268) proposes a method of measuring software quality by analysing the code smells in a project
  - I will be combining these two approaches to create a more comprehensive method of measuring software quality -->

- Targeting: C#
  - Object Oriented
  - Popular
  - Confident in the language

- Solution: A tool that will analyze a codebase and format it to a schema while making use of OOP principles
  - Inputs:
    - Codebase
    - Schema
  - Outputs:
    - Formatted codebase
    - UML diagram
    - Report of what has changed and documentation

- Purpose: To provide a standardised way of measuring software quality
  - Useful for teams to maintain a specific standard
  - Useful as a teaching aid to help students learn how to write high quality code (as proven by (ACM-10.1145_3428029.3428047))

- Reason:
  - As indicated in the problem domain there are many proposed solutions but few adopted ones (IEEE-6606742).
  - My solution does not aim to solve any one specific problem set but instead aims to reduce the impact of poorly maintained code.
  - [Reference] "no tool succeeds in all respects" (IEEE-8681007)

### Evaluation

By reference to the Journals, identify how you will evaluate / measure the success of your project. What is your evaluation path? What will be the metrics of your product’s success?
References
In Harvard Style

- Produced code should maintain the function and result of the original code
  - Measured by comparing the output of the original code and the output of the formatted code through manual and automated testing
- Produced code should not be any harder to digest than the original code
  - Measured by peer review
  - Measured by external complexity measuring tools
- Peformance should be on-par or better than the original code
  - Measured through testing and execution time
  - While I won't aim to optimise the codes performance, I will aim to not make it worse
