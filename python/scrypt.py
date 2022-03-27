import sys
import cv2
import numpy as np
import matplotlib.pyplot as plt

image = cv2.imread(sys.argv[1])
output = image.copy()
height, width = image.shape[:2]
# minRadius = int(0.9*(width/12)/2 / 5.5) # the higier last number the lower min radius
# maxRadius = int(1.1*(width/12)/2 / 6.75) # the higher last number the lower max radius

minRadius = 32
maxRadius = 35

# minRadius 33 and maxradius 34 seems to be optimal

gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
circles = cv2.HoughCircles(image=gray, 
                           method=cv2.HOUGH_GRADIENT, 
                           dp=1.2, 
                           minDist=2*minRadius,
                           param1=50,
                           param2=50,
                           minRadius=minRadius,
                           maxRadius=maxRadius                           
                          )

if circles is not None:
    # convert the (x, y) coordinates and radius of the circles to integers
    circlesRound = np.round(circles[0, :]).astype("int")
    # loop over the (x, y) coordinates and radius of the circles
    for (x, y, r) in circlesRound:
        cv2.circle(output, (x, y), r, (0, 255, 0), 4)
        print(x, y, r)

    plt.figure(figsize=(40, 30), dpi=80)
    plt.imshow(output, cmap = 'gray', interpolation = 'bicubic')
    plt.xticks([]), plt.yticks([])  # to hide tick values on X and Y axis
    plt.show()
else:
    print ('No circles found')
