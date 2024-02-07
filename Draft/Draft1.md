### Introduction

This project aims to provide a solution to the problem of creating high quality code and structured code. Research was made into the area of software quality and the different approaches that have been proposed to measure software quality. It was found that there is a lack of standardisation in the way software quality is measured and that there is a demand for high quality code. It was also found that many of the proposed approaches to measuring software quality have not been widely adopted. This project aims to provide a solution to this problem by creating a tool that can analyze source code and format it to a schema while making use of object oriented programming principles.

### Problem Domain

The object oriented programming paradigm has become one of the most popular programming paradigms in the industry (IEEE-6606742). The wide adoptation of this paradigm has resulted in many large projects being written in an object oriented language. Unfortuantly poorly written code can be hard to maintain, and as a project grows in size it can even become too expensive to continue development (IEEE-8802820, ACM-10.1145_2507288.2507312, ACM-10.1145_3379597.3387457, IEEE-6606742, IEEE-7372958). As a result of this there is a demand for high quality code (IEEE-6606742).
Research indicates that there is a lack of standardisation in the way software quality is measured (IEEE-6606742, IEEE-8681007). This can make it difficult for developers to decide what methodologies to adopt (IEEE-6606742). Many of the papers in this area go over how important software quality is and the problems around the topic but only few provide approaches and solutions to solve the issues. This is also backed up by the research conducted by (IEEE-6606742) where they found that there is a demand for high quality code and a surplus of therotical solutions but a lack of real world solutions.

Further research into the teaching industry also shows that teachers are struggling to teach students how to write high quality code (ACM-10.1145_3428029.3428047). This can have a knock on effect into the industry as students will be entering the industry with a lack of understanding of how to write high quality code which again, can result in projects becoming hard to maintain, overrunning and becoming too expensive to keep up to date (IEEE-8802820, ACM-10.1145_2507288.2507312, ACM-10.1145_3379597.3387457, IEEE-6606742, IEEE-7372958).

The problem here then is that there have been a lack of real world solutions to the problem of software quality.

### Methodology

This project will aim to provide a solution to the problem of software quality by creating a tool that can analyze source code and format it to a schema while making use of object oriented programming principles.
The tool will be targeting the C# language as it is an object oriented language that is popular in the industry.

Such a tool will require various inputs and outputs of data. As shown in previous studies, there are many ways to measure software quality. In this regard software quality is a broad topic (IEEE-6606742) and as there is no standardised way to measure software quality (IEEE-6606742, IEEE-8681007) we cannot just create a tool that cohears to a standard. To work around this one of out inputs will be a schema that the user can define. This schema will be used to define the structure of the codebase and the tool will then format the codebase to match this schema. The tool will also make use of object oriented programming principles so that the advantages of this paradigm are not put to waste. Ontop of this as we are focusing the C# language we can tune the tool to make use of the features of this language.

Output data will be the formatted codebase and a report on the quality of the codebase. The report is an additional important piece of information as it can help developers to see what has changed and the new structure of the codebase. The importance of this can come in handy when migrating a codebase to a new development team as it can help the new team to understand the codebase. Such a report could contain UML diagrams as a visual overview of the codebase as well as documentation, which is a very important factor to software quality (ACM-10.1145_3428029.3428047).

In the paper (IEEE-8681007) when they were evaluating some of the existing tools they said "no tool succeeds in all respects". The tool that this paper proposes will by no means succeed any other tool in all respects, nor will it aim to specifically solve a specific problemset of software quality. Instead this tool will aim to reduce the impact of poorly written code by providing a way to structure codebases as well as being useful as a learning tool.

### Evaluation

In order to evaulate this tool we will take into account the following;
The reproduced code should maintain the function and result of the original code. We can measure this by running the original code and the reproduced code either through manual or automated testing and then comparing the results.
The reproduced code should not be any harder to digest than the original code. This is a harder metric to measure as it is subjective. So in order to evaulate this we can use peer review as well as external complexity measuring tools.
Finally the peformance of the reproduced code should be on-par or better than the original code. This can be measured by running the original code and the reproduced code and comparing the time taken to run each. While this tool will not aim to optimise the performance of the code, it should not make it worse.
